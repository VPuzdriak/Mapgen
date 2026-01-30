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

    // Analyze constructors before property mappings
    AnalyzeConstructors(classNode, methodMetadata, ct);

    AddIgnoreMappings(classNode, methodMetadata, ct);
    AddCustomMappings(classNode, methodMetadata, ct);
    AddCollectionMappings(classNode, methodMetadata, ct);
    AddDirectMappings(methodMetadata, ct);
    AddUnmappedMemberDiagnostics(methodMetadata, ct);

    // Detect required usings
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
    ProcessUnmappedMembers(methodMetadata, ct, destMember =>
    {
      if (methodMetadata.SourceObjectParameter.Symbol.Type is not INamedTypeSymbol sourceType)
      {
        return;
      }

      // Look for matching source member by name (case-sensitive)
      var sourceMember = sourceType.GetAllMembers()
        .FirstOrDefault(sm => sm.Name == destMember.Name);

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
        Constants.MapMemberMethodName,
        Constants.IgnoreMemberMethodName,
        memberType);

      methodMetadata.AddDiagnostic(diagnostic);
    }
  }

  private void AnalyzeConstructors(SyntaxNode classNode, MapperMethodMetadata methodMetadata, CancellationToken ct)
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

    // If user specified UseEmptyConstructor, validate it's possible
    if (hasUseEmptyConstructorCall)
    {
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
      return;
    }

    // If user specified UseConstructor, parse it
    if (hasUseConstructorCall)
    {
      var constructorArgs = _constructorMappingStrategy.ParseConstructorArguments(classNode, methodMetadata, ct);
      var selectedConstructor = _constructorMappingStrategy.SelectConstructorByParameterCount(destinationType, constructorArgs.Count);

      if (selectedConstructor != null)
      {
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

      return;
    }

    // No explicit constructor configuration - analyze and apply auto-mapping if possible

    var totalConstructorCount = publicConstructors.Count;

    // Case 1: Exactly 1 constructor - try to auto-use it
    if (totalConstructorCount == 1)
    {
      var singleConstructor = publicConstructors[0];

      // If it's parameterless, no need to do anything (will use it automatically)
      if (singleConstructor.Parameters.Length == 0)
      {
        return;
      }

      // If it's parameterized, try to auto-map
      if (methodMetadata.SourceObjectParameter.Symbol.Type is INamedTypeSymbol sourceType &&
          _constructorMappingStrategy.CanAutoMapConstructor(singleConstructor, sourceType))
      {
        // Auto-map the constructor
        var constructorInfo = new ConstructorInfo(singleConstructor);
        methodMetadata.SetConstructorInfo(constructorInfo);

        // Create constructor argument mappings for each parameter
        foreach (var parameter in singleConstructor.Parameters)
        {
          // Find matching source member (case-insensitive)
          var sourceMember = sourceType.GetAllMembers()
            .FirstOrDefault(m => string.Equals(m.Name, parameter.Name, StringComparison.OrdinalIgnoreCase));

          if (sourceMember == null)
          {
            continue;
          }

          var sourceExpression = $"{methodMetadata.SourceObjectParameter.Name}.{sourceMember.Name}";
          var constructorArgMapping = new ConstructorArgumentDescriptor(
            parameter.Name,
            sourceExpression,
            parameter.Ordinal);

          methodMetadata.AddMapping(constructorArgMapping);
        }

        return; // Successfully auto-mapped
      }

      // Can't auto-map single parameterized constructor - show diagnostic
      var signatures = ConstructorMappingStrategy.GetConstructorSignatures(destinationType);
      var diagnostic = MapperDiagnostic.ParameterizedConstructorRequired(
        methodMetadata.MethodSymbol.Locations.FirstOrDefault(),
        destinationType.Name,
        signatures);

      methodMetadata.AddDiagnostic(diagnostic);
      return;
    }

    // Case 2: Multiple constructors (2+) - ERROR: ambiguous, must specify which to use
    if (totalConstructorCount > 1)
    {
      var signatures = ConstructorMappingStrategy.GetConstructorSignatures(destinationType);
      var diagnostic = MapperDiagnostic.AmbiguousConstructorSelection(
        methodMetadata.MethodSymbol.Locations.FirstOrDefault(),
        destinationType.Name,
        signatures);

      methodMetadata.AddDiagnostic(diagnostic);
    }

    // Case 3: No public constructors (very rare, but handle it)
    // No diagnostic needed - other error will occur during generation
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
