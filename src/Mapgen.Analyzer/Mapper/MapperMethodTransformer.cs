using System.Linq;
using System.Threading;

using Mapgen.Analyzer.Mapper.Diagnostics;
using Mapgen.Analyzer.Mapper.MappingDescriptors;
using Mapgen.Analyzer.Mapper.Metadata;
using Mapgen.Analyzer.Mapper.Strategies;

using Microsoft.CodeAnalysis;

namespace Mapgen.Analyzer.Mapper;

public sealed class MapperMethodTransformer(SemanticModel semanticModel)
{
  private readonly DirectMappingStrategy _directMappingStrategy = new(semanticModel);
  private readonly IgnoreMappingStrategy _ignoreMappingStrategy = new();
  private readonly CustomMappingStrategy _customMappingStrategy = new();
  private readonly CollectionMappingStrategy _collectionMappingStrategy = new();
  private readonly MappingParser _mappingParser = new(semanticModel);

  public MapperMethodMetadata? Transform(IMethodSymbol method, SyntaxNode classNode, CancellationToken ct)
  {
    var methodMetadata = new MapperMethodMetadata(method);

    ParseIncludedMappers(classNode, methodMetadata, ct);
    AddIgnoreMappings(classNode, methodMetadata, ct);
    AddCustomMappings(classNode, methodMetadata, ct);
    AddCollectionMappings(classNode, methodMetadata, ct);
    AddDirectMappings(methodMetadata, ct);
    AddUnmappedPropertyDiagnostics(methodMetadata, ct);

    return methodMetadata;
  }

  private void ParseIncludedMappers(SyntaxNode classNode, MapperMethodMetadata methodMetadata, CancellationToken ct)
  {
    var includedMappers = _mappingParser.ParseIncludedMappers(classNode, ct);

    foreach (var includedMapper in includedMappers)
    {
      methodMetadata.AddIncludedMapper(includedMapper);
    }
  }

  private void AddIgnoreMappings(SyntaxNode classNode, MapperMethodMetadata methodMetadata, CancellationToken ct)
  {
    var ignoredPropertyMappings = _ignoreMappingStrategy.ParseIgnoreMappings(classNode, ct);

    foreach (var destPropertySymbol in methodMetadata.ReturnType.GetMembers().OfType<IPropertySymbol>())
    {
      if (ct.IsCancellationRequested)
      {
        break;
      }

      // Skip properties that should be automatically ignored
      if (ShouldIgnoreProperty(destPropertySymbol))
      {
        methodMetadata.AddMapping(new IgnoredPropertyDescriptor(destPropertySymbol.Name, null));
        continue;
      }

      if (ignoredPropertyMappings.TryGetValue(destPropertySymbol.Name, out var ignoredPropertyMapping))
      {
        // Check if the property has the 'required' keyword
        if (destPropertySymbol.IsRequired)
        {
          var diagnostic = MapperDiagnostic.RequiredMemberCannotBeIgnored(
            ignoredPropertyMapping.IgnoreMemberMethodCallLocation,
            destPropertySymbol.Name);
          
          methodMetadata.AddDiagnostic(diagnostic);
          methodMetadata.AddMapping(new DiagnosedPropertyDescriptor(destPropertySymbol.Name));
          continue;
        }

        methodMetadata.AddMapping(ignoredPropertyMapping);
      }
    }
  }

  private void AddCustomMappings(SyntaxNode classNode, MapperMethodMetadata methodMetadata, CancellationToken ct)
  {
    var customMappings = _customMappingStrategy.ParseCustomMappings(classNode, methodMetadata, ct);

    foreach (var destPropertySymbol in methodMetadata.ReturnType.GetMembers().OfType<IPropertySymbol>())
    {
      if (ct.IsCancellationRequested)
      {
        break;
      }
      // Check if there's a custom mapping for this property
      // If yes - skip. It has already been handled
      if (methodMetadata.Mappings.Any(m => m.TargetPropertyName == destPropertySymbol.Name))
      {
        continue;
      }

      if (customMappings.TryGetValue(destPropertySymbol.Name, out var customMapping))
      {
        methodMetadata.AddMapping(customMapping);
      }
    }
  }

  private void AddCollectionMappings(SyntaxNode classNode, MapperMethodMetadata methodMetadata, CancellationToken ct)
  {
    var collectionMappings = _collectionMappingStrategy.ParseCollectionMappings(classNode, methodMetadata, ct);

    foreach (var destPropertySymbol in methodMetadata.ReturnType.GetMembers().OfType<IPropertySymbol>())
    {
      if (ct.IsCancellationRequested)
      {
        break;
      }

      // Check if there's a mapping for this property (ignore, custom, or collection)
      // If yes - skip. It has already been handled
      if (methodMetadata.Mappings.Any(m => m.TargetPropertyName == destPropertySymbol.Name))
      {
        continue;
      }

      if (collectionMappings.TryGetValue(destPropertySymbol.Name, out var collectionMapping))
      {
        methodMetadata.AddMapping(collectionMapping);
      }
    }
  }

  private void AddDirectMappings(MapperMethodMetadata methodMetadata, CancellationToken ct)
  {
    foreach (var destPropertySymbol in methodMetadata.ReturnType.GetMembers().OfType<IPropertySymbol>())
    {
      if (ct.IsCancellationRequested)
      {
        break;
      }
      // Check if there's a custom mapping for this property
      // If yes - skip. It has already been handled
      if (methodMetadata.Mappings.Any(m => m.TargetPropertyName == destPropertySymbol.Name))
      {
        continue;
      }

      var sourcePropertySymbol = methodMetadata.SourceObjectParameter.Symbol.Type.GetMembers()
        .OfType<IPropertySymbol>()
        .FirstOrDefault(sp => sp.Name == destPropertySymbol.Name);

      // Same property found by name - delegate to direct mapping strategy
      if (sourcePropertySymbol is not null)
      {
        var mapping = _directMappingStrategy.TryCreateDirectMapping(sourcePropertySymbol, destPropertySymbol, methodMetadata);
        methodMetadata.AddMapping(mapping);
      }
    }
  }

  private void AddUnmappedPropertyDiagnostics(MapperMethodMetadata methodMetadata, CancellationToken ct)
  {
    var returnType = methodMetadata.ReturnType;
    var sourceType = methodMetadata.SourceObjectParameter.Symbol.Type;

    foreach (var destProperty in returnType.GetMembers().OfType<IPropertySymbol>())
    {
      if (ct.IsCancellationRequested)
      {
        break;
      }

      // Check if property is mapped or ignored
      var isMapped = methodMetadata.Mappings.Any(m => m.TargetPropertyName == destProperty.Name);

      if (!isMapped)
      {
        // Property is not mapped and not ignored - create diagnostic
        var diagnostic = MapperDiagnostic.MissingPropertyMapping(
          methodMetadata.MethodSymbol.Locations.FirstOrDefault(),
          returnType.Name,
          destProperty.Name,
          sourceType.Name,
          Constants.MapMemberMethodName,
          Constants.IgnoreMemberMethodName);

        methodMetadata.AddDiagnostic(diagnostic);
      }
    }
  }

  /// <summary>
  /// Determines if a property should be automatically ignored during mapping.
  /// </summary>
  /// <param name="property">The property to check</param>
  /// <returns>True if the property should be ignored, false otherwise</returns>
  private static bool ShouldIgnoreProperty(IPropertySymbol property)
  {
    // Ignore EqualityContract property from record types
    // This is a compiler-generated property that shouldn't be mapped
    if (property.Name == "EqualityContract" && property.ContainingType.IsRecord)
    {
      return true;
    }

    return false;
  }
}
