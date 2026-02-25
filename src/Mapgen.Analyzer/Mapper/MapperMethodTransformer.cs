using System;
using System.Linq;
using System.Threading;

using Mapgen.Analyzer.Mapper.Diagnostics;
using Mapgen.Analyzer.Mapper.MappingDescriptors;
using Mapgen.Analyzer.Mapper.Metadata;
using Mapgen.Analyzer.Mapper.Strategies;
using Mapgen.Analyzer.Mapper.Utils;

using Microsoft.CodeAnalysis;

namespace Mapgen.Analyzer.Mapper;

public sealed class MapperMethodTransformer(SemanticModel semanticModel)
{
  private readonly DirectMappingStrategy _directMappingStrategy = new(semanticModel);
  private readonly IgnoreMappingStrategy _ignoreMappingStrategy = new();
  private readonly CustomMappingStrategy _customMappingStrategy = new();
  private readonly CollectionMappingStrategy _collectionMappingStrategy = new();
  private readonly ConstructorMappingStrategy _constructorMappingStrategy = new(semanticModel);
  private readonly MappingParser _mappingParser = new(semanticModel);

  public MapperMethodMetadata? Transform(IMethodSymbol method, SyntaxNode classNode, CancellationToken ct)
  {
    if (!TryCreateMethodMetadata(method, out var methodMetadata) || methodMetadata is null)
    {
      return null;
    }

    ParseIncludedMappers(classNode, methodMetadata, ct);

    AddConstructorArgumentsMappings(classNode, methodMetadata, ct);
    AddIgnoreMappings(classNode, methodMetadata, ct);
    AddCustomMappings(classNode, methodMetadata, ct);
    AddCollectionMappings(classNode, methodMetadata, ct);
    AddDirectMappings(methodMetadata, ct);
    AddUnmappedMemberDiagnostics(methodMetadata, ct);

    DetectRequiredUsings(methodMetadata);

    return methodMetadata;
  }

  private bool TryCreateMethodMetadata(IMethodSymbol methodSymbol, out MapperMethodMetadata? methodMetadata)
  {
    var methodDeclarationSyntax = SyntaxHelpers.FindMethodDeclaration(methodSymbol);

    // Method declaration was not found - cannot proceed
    if (methodDeclarationSyntax is null)
    {
      methodMetadata = null;
      return false;
    }

    methodMetadata = new MapperMethodMetadata(methodSymbol, methodDeclarationSyntax);
    return true;
  }

  private void ParseIncludedMappers(SyntaxNode classNode, MapperMethodMetadata methodMetadata, CancellationToken ct)
  {
    var includedMappers = _mappingParser.ParseIncludedMappers(classNode, ct);

    foreach (var includedMapper in includedMappers)
    {
      methodMetadata.AddIncludedMapper(includedMapper);
    }
  }

  /// <summary>
  /// Helper method to iterate through unmapped destination members (properties and fields) and apply an action.
  /// </summary>
  private void ProcessUnmappedMembers(
    MapperMethodMetadata methodMetadata,
    CancellationToken ct,
    Action<MemberInfo> action)
  {
    foreach (var destMember in methodMetadata.ReturnType.GetAllMembers())
    {
      if (ct.IsCancellationRequested)
      {
        break;
      }

      // Skip if already mapped
      if (methodMetadata.Mappings.Any(m => m.TargetMemberName == destMember.Name))
      {
        continue;
      }

      action(destMember);
    }
  }

  /// <summary>
  /// Tries to get the source type from the method metadata.
  /// </summary>
  private static bool TryGetSourceType(MapperMethodMetadata methodMetadata, out INamedTypeSymbol? sourceType)
  {
    sourceType = methodMetadata.SourceObjectParameter.Symbol.Type as INamedTypeSymbol;
    return sourceType is not null;
  }

  /// <summary>
  /// Finds a matching source member by name with the specified comparison type.
  /// </summary>
  private static MemberInfo? FindMatchingSourceMember(
    INamedTypeSymbol sourceType,
    string memberName,
    StringComparison comparison = StringComparison.Ordinal)
  {
    return sourceType.GetAllMembers()
      .FirstOrDefault(m => string.Equals(m.Name, memberName, comparison));
  }

  /// <summary>
  /// Determines if a readonly member should be automatically ignored.
  /// Members without setters (readonly/computed properties, readonly fields) should always be ignored
  /// since they cannot be set via object initializer. If they're constructor parameters,
  /// they'll already be in the mappings list and won't reach this check.
  /// </summary>
  private static bool ShouldIgnoreReadonlyMember(MemberInfo member)
  {
    // Member without setter should always be ignored
    // (computed properties, expression-bodied properties, get-only properties, readonly fields)
    // If it's a constructor parameter, it's already mapped and won't reach this method
    return !member.IsSettable();
  }

  private void AddIgnoreMappings(SyntaxNode classNode, MapperMethodMetadata methodMetadata, CancellationToken ct)
  {
    var ignoredPropertyMappings = _ignoreMappingStrategy.ParseIgnoreMappings(classNode, ct);

    ProcessUnmappedMembers(methodMetadata, ct, destMember =>
    {
      // Skip members that should be automatically ignored (system properties)
      if (destMember.IsProperty && destMember.AsProperty()!.IsSystem())
      {
        methodMetadata.AddMapping(new IgnoredPropertyDescriptor(destMember.Name, null));
        return;
      }

      // Handle readonly members - only ignore if no constructor mapping configured
      if (ShouldIgnoreReadonlyMember(destMember))
      {
        methodMetadata.AddMapping(new IgnoredPropertyDescriptor(destMember.Name, null));
        return;
      }

      if (ignoredPropertyMappings.TryGetValue(destMember.Name, out var ignoredPropertyMapping))
      {
        // Check if the member is required (only properties can be required)
        if (destMember.IsRequired())
        {
          var diagnostic = MapperDiagnostic.RequiredMemberCannotBeIgnored(
            ignoredPropertyMapping.IgnoreMemberMethodCallLocation,
            destMember.Name);

          methodMetadata.AddDiagnostic(diagnostic);
          methodMetadata.AddMapping(new DiagnosedPropertyDescriptor(destMember.Name));
          return;
        }

        methodMetadata.AddMapping(ignoredPropertyMapping);
      }
    });
  }

  private void AddCustomMappings(SyntaxNode classNode, MapperMethodMetadata methodMetadata, CancellationToken ct)
  {
    var customMappings = _customMappingStrategy.ParseCustomMappings(classNode, methodMetadata, ct);

    ProcessUnmappedMembers(methodMetadata, ct, destMember =>
    {
      if (customMappings.TryGetValue(destMember.Name, out var customMapping))
      {
        methodMetadata.AddMapping(customMapping);
      }
    });
  }

  private void AddCollectionMappings(SyntaxNode classNode, MapperMethodMetadata methodMetadata, CancellationToken ct)
  {
    var collectionMappings = _collectionMappingStrategy.ParseCollectionMappings(classNode, methodMetadata, ct);

    ProcessUnmappedMembers(methodMetadata, ct, destMember =>
    {
      if (collectionMappings.TryGetValue(destMember.Name, out var collectionMapping))
      {
        methodMetadata.AddMapping(collectionMapping);
      }
    });
  }

  private void AddDirectMappings(MapperMethodMetadata methodMetadata, CancellationToken ct)
  {
    if (!TryGetSourceType(methodMetadata, out var sourceType) || sourceType is null)
    {
      return;
    }

    ProcessUnmappedMembers(methodMetadata, ct, destMember =>
    {
      // Look for matching source member by name (case-sensitive)
      var sourceMember = FindMatchingSourceMember(sourceType, destMember.Name);

      if (sourceMember is null)
      {
        return;
      }

      // Skip source members without read access (write-only properties, non-readable fields)
      if (!sourceMember.IsReadable())
      {
        return; // Don't create mapping for unreadable source member
      }

      // Create direct mapping for matching members
      var mapping = _directMappingStrategy.TryCreateDirectMapping(sourceMember, destMember, methodMetadata);
      methodMetadata.AddMapping(mapping);
    });
  }

  private void AddUnmappedMemberDiagnostics(MapperMethodMetadata methodMetadata, CancellationToken ct)
  {
    var returnType = methodMetadata.ReturnType;
    var sourceType = methodMetadata.SourceObjectParameter.Symbol.Type;

    foreach (var destMember in returnType.GetAllMembers())
    {
      if (ct.IsCancellationRequested)
      {
        break;
      }

      // Check if member is mapped or ignored
      var isMapped = methodMetadata.Mappings.Any(m => m.TargetMemberName == destMember.Name);

      if (isMapped)
      {
        continue;
      }

      // Member is not mapped and not ignored - create diagnostic
      var memberType = destMember.IsField ? "field" : "property";
      var diagnostic = MapperDiagnostic.MissingPropertyMapping(
        methodMetadata.MethodSymbol.Locations.FirstOrDefault(),
        returnType.Name,
        destMember.Name,
        sourceType.Name,
        MappingConfigurationMethods.MapMemberMethodName,
        MappingConfigurationMethods.IgnoreMemberMethodName,
        memberType);

      methodMetadata.AddDiagnostic(diagnostic);
    }
  }

  private void AddConstructorArgumentsMappings(SyntaxNode classNode, MapperMethodMetadata methodMetadata, CancellationToken ct)
  {
    var destinationType = methodMetadata.ReturnType;

    // Check if user specified UseConstructor() or UseEmptyConstructor()
    var hasUseConstructorCall = _constructorMappingStrategy.HasUseConstructorCall(classNode);
    var hasUseEmptyConstructorCall = _constructorMappingStrategy.HasUseEmptyConstructorCall(classNode);

    // Get all public constructors
    var publicConstructors = destinationType.InstanceConstructors
      .Where(c => c.DeclaredAccessibility == Accessibility.Public)
      .ToList();

    if (publicConstructors.Count == 0)
    {
      // No public constructors - can't map (very rare)
      return;
    }

    if (TryGetSourceType(methodMetadata, out var sourceType) && sourceType is not null)
    {
      foreach (var constructor in publicConstructors)
      {
        _constructorMappingStrategy.GenerateEnumMappingsForConstructor(constructor, sourceType, methodMetadata);
      }
    }

    // If user specified UseEmptyConstructor, validate it's possible
    if (hasUseEmptyConstructorCall)
    {
      HandleUseEmptyConstructorCall(classNode, methodMetadata, publicConstructors);
      return;
    }

    // If user specified UseConstructor, parse it
    if (hasUseConstructorCall)
    {
      HandleUseConstructorCall(classNode, methodMetadata, ct);
      return;
    }

    // No explicit constructor configuration - analyze and apply auto-mapping if possible
    var totalConstructorCount = publicConstructors.Count;

    if (totalConstructorCount == 1)
    {
      HandleSingleConstructor(methodMetadata, publicConstructors[0]);
      return;
    }

    if (totalConstructorCount > 1)
    {
      HandleMultipleConstructors(methodMetadata);
    }
  }

  private void HandleUseEmptyConstructorCall(
    SyntaxNode classNode,
    MapperMethodMetadata methodMetadata,
    System.Collections.Generic.List<IMethodSymbol> publicConstructors)
  {
    var destinationType = methodMetadata.ReturnType;
    var parameterlessConstructor = publicConstructors.FirstOrDefault(c => c.Parameters.Length == 0);

    if (parameterlessConstructor == null)
    {
      // ERROR: UseEmptyConstructor called but no parameterless constructor exists
      var signatures = ConstructorMappingStrategy.GetConstructorSignatures(destinationType);
      var callLocation = _constructorMappingStrategy.GetUseEmptyConstructorCallLocation(classNode);
      var diagnostic = MapperDiagnostic.UseEmptyConstructorNotPossible(
        callLocation ?? methodMetadata.MethodSymbol.Locations.FirstOrDefault(),
        destinationType.Name,
        signatures);

      methodMetadata.AddDiagnostic(diagnostic);
      return;
    }

    // Valid: UseEmptyConstructor and parameterless constructor exists
    methodMetadata.SetUseEmptyConstructor(true);
  }

  private void HandleUseConstructorCall(
    SyntaxNode classNode,
    MapperMethodMetadata methodMetadata,
    CancellationToken ct)
  {
    var destinationType = methodMetadata.ReturnType;
    var constructorArgs = _constructorMappingStrategy.ParseConstructorArguments(classNode, methodMetadata, ct);
    var selectedConstructor = _constructorMappingStrategy.SelectConstructorByParameterCount(destinationType, constructorArgs.Count);

    if (selectedConstructor == null)
    {
      return;
    }

    var constructorInfo = new ConstructorInfo(selectedConstructor);
    methodMetadata.SetConstructorInfo(constructorInfo);

    // Create constructor argument mappings
    for (var i = 0; i < constructorArgs.Count && i < selectedConstructor.Parameters.Length; i++)
    {
      var parameter = selectedConstructor.Parameters[i];
      var sourceExpression = constructorArgs[i];

      var constructorArgMapping = new ConstructorArgumentDescriptor(
        parameter.Name,
        sourceExpression,
        i);

      methodMetadata.AddMapping(constructorArgMapping);
    }
  }

  private void HandleSingleConstructor(MapperMethodMetadata methodMetadata, IMethodSymbol singleConstructor)
  {
    // If it's parameterless, no need to do anything (will use it automatically)
    if (singleConstructor.Parameters.Length == 0)
    {
      return;
    }

    // If it's parameterized, try to auto-map
    if (!TryGetSourceType(methodMetadata, out var sourceType) || sourceType is null ||
        !_constructorMappingStrategy.CanAutoMapConstructor(singleConstructor, sourceType, methodMetadata))
    {
      // Can't auto-map single parameterized constructor - show diagnostic
      var signatures = ConstructorMappingStrategy.GetConstructorSignatures(methodMetadata.ReturnType);
      var diagnostic = MapperDiagnostic.ParameterizedConstructorRequired(
        methodMetadata.MethodSymbol.Locations.FirstOrDefault(),
        methodMetadata.ReturnType.Name,
        signatures);

      methodMetadata.AddDiagnostic(diagnostic);
      return;
    }

    // Auto-map the constructor
    var constructorInfo = new ConstructorInfo(singleConstructor);
    methodMetadata.SetConstructorInfo(constructorInfo);

    // Create constructor argument mappings for each parameter
    foreach (var parameter in singleConstructor.Parameters)
    {
      // Find matching source member (case-insensitive)
      var sourceMember = FindMatchingSourceMember(sourceType, parameter.Name, StringComparison.OrdinalIgnoreCase);

      if (sourceMember == null)
      {
        continue;
      }

      // Build the source expression using included mapper if needed
      var sourceExpression = _constructorMappingStrategy.BuildConstructorArgumentExpression(
        sourceMember,
        parameter.Type,
        methodMetadata);

      var constructorArgMapping = new ConstructorArgumentDescriptor(
        parameter.Name,
        sourceExpression,
        parameter.Ordinal);

      methodMetadata.AddMapping(constructorArgMapping);
    }
  }

  private void HandleMultipleConstructors(MapperMethodMetadata methodMetadata)
  {
    // Multiple constructors (2+) - ERROR: ambiguous, must specify which to use
    var signatures = ConstructorMappingStrategy.GetConstructorSignatures(methodMetadata.ReturnType);
    var diagnostic = MapperDiagnostic.AmbiguousConstructorSelection(
      methodMetadata.MethodSymbol.Locations.FirstOrDefault(),
      methodMetadata.ReturnType.Name,
      signatures);

    methodMetadata.AddDiagnostic(diagnostic);
  }


  private static void DetectRequiredUsings(MapperMethodMetadata methodMetadata)
  {
    var sourceMappings = methodMetadata.Mappings.OfType<SourceMappingDescriptor>().ToList();

    // Check if any mapping uses immutable collection methods
    var usesImmutableCollections = sourceMappings
      .Any(m => m.SourceExpression.Contains(".ToImmutable"));

    if (usesImmutableCollections)
    {
      methodMetadata.AddRequiredUsing("System.Collections.Immutable");
    }

    // Check if any mapping uses LINQ methods (Select, Where, etc.)
    var usesLinq = sourceMappings
      .Any(m => m.SourceExpression.Contains(".Select(") ||
                m.SourceExpression.Contains(".Where(") ||
                m.SourceExpression.Contains(".ToList(") ||
                m.SourceExpression.Contains(".ToArray(") ||
                m.SourceExpression.Contains(".ToHashSet("));

    if (usesLinq)
    {
      methodMetadata.AddRequiredUsing("System.Linq");
    }
  }
}
