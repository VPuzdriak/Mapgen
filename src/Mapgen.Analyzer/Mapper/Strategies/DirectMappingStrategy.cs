using System.Collections.Generic;
using System.Linq;

using Mapgen.Analyzer.Mapper.Diagnostics;
using Mapgen.Analyzer.Mapper.MappingDescriptors;
using Mapgen.Analyzer.Mapper.Metadata;
using Mapgen.Analyzer.Mapper.Utils;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Mapgen.Analyzer.Mapper.Strategies;

public sealed class DirectMappingStrategy(SemanticModel semanticModel)
{
  public BaseMappingDescriptor TryCreateDirectMapping(
    MemberInfo sourceMember,
    MemberInfo destMember,
    MapperMethodMetadata methodMetadata)
  {
    // Check if types are compatible (exact match including nullable annotations or implicit conversion exists)
    var typesMatchIgnoringNullability =
      SymbolEqualityComparer.Default.Equals(sourceMember.Type, destMember.Type);
    var nullabilityMatches = sourceMember.Type.NullableAnnotation == destMember.Type.NullableAnnotation;
    var typesMatch = typesMatchIgnoringNullability && nullabilityMatches;

    if (typesMatch)
    {
      // Exact match - create direct mapping
      return CreateSuccessfulMapping(sourceMember, destMember, methodMetadata);
    }

    // Types don't match - check for special cases
    if (IsNullableToNonNullableMismatch(sourceMember, destMember, typesMatchIgnoringNullability))
    {
      return CreateNullableMismatchDiagnostic(sourceMember, destMember, methodMetadata);
    }

    // Check if there's an implicit conversion from source to destination type (for different types like short -> int)
    if (HasImplicitConversion(sourceMember.Type, destMember.Type))
    {
      return CreateSuccessfulMapping(sourceMember, destMember, methodMetadata);
    }

    // Check if both are collections with compatible element types
    var collectionMapping = TryCreateCollectionMappingWithCompatibleElements(
      sourceMember,
      destMember,
      methodMetadata);
    if (collectionMapping is not null)
    {
      return collectionMapping;
    }

    // Check if there's an included mapper that can map from source type to destination type
    var includedMapperMapping =
      TryCreateMappingWithIncludedMapper(sourceMember, destMember, methodMetadata);
    if (includedMapperMapping is not null)
    {
      return includedMapperMapping;
    }

    // No valid mapping possible - report type mismatch
    return CreateTypeMismatchDiagnostic(sourceMember, destMember, methodMetadata);
  }

  private bool IsNullableToNonNullableMismatch(
    MemberInfo sourceMember,
    MemberInfo destMember,
    bool typesMatchIgnoringNullability)
  {
    if (!typesMatchIgnoringNullability)
    {
      return false;
    }

    var sourceIsNullable = sourceMember.Type.NullableAnnotation == NullableAnnotation.Annotated;
    var destIsNullable = destMember.Type.NullableAnnotation == NullableAnnotation.Annotated;

    return sourceIsNullable && !destIsNullable;
  }

  private bool HasImplicitConversion(ITypeSymbol sourceType, ITypeSymbol destType)
  {
    if (semanticModel.Compilation is not CSharpCompilation csharpCompilation)
    {
      return false;
    }

    var conversion = csharpCompilation.ClassifyConversion(sourceType, destType);
    return conversion is { IsImplicit: true, IsUserDefined: false };
  }

  private MappingDescriptor? TryCreateCollectionMappingWithCompatibleElements(
    MemberInfo sourceMember,
    MemberInfo destMember,
    MapperMethodMetadata methodMetadata)
  {
    // Extract element types if these are collection types
    var sourceElementType = CollectionHelpers.GetCollectionElementType(sourceMember.Type);
    var destElementType = CollectionHelpers.GetCollectionElementType(destMember.Type);

    // Both must be collections
    if (sourceElementType is null || destElementType is null)
    {
      return null;
    }

    // Check if element types are directly compatible (exact match or implicit conversion)
    var elementTypesMatch = SymbolEqualityComparer.Default.Equals(sourceElementType, destElementType);
    var hasImplicitConversion = !elementTypesMatch && HasImplicitConversion(sourceElementType, destElementType);

    if (!elementTypesMatch && !hasImplicitConversion)
    {
      // Element types are not compatible
      return null;
    }

    // Element types are compatible - generate collection mapping
    var sourceExpression = $"{methodMetadata.SourceObjectParameter.Name}.{sourceMember.Name}";
    var itemParamName = CollectionHelpers.GetItemParameterName(sourceElementType);

    // When element types differ (even with implicit conversion), we need explicit cast
    // because generic interfaces are invariant in C#.
    // Example: List<short> -> IImmutableList<long>
    // Without cast: Select(x => x).ToImmutableList() returns ImmutableList<short>
    // With cast: Select(x => (long)x).ToImmutableList() returns ImmutableList<long>
    var itemTransformExpression = hasImplicitConversion
      ? $"({destElementType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}){itemParamName}"
      : itemParamName;

    var mappingExpression = CollectionHelpers.BuildCollectionMappingExpression(
      sourceExpression,
      itemParamName,
      itemTransformExpression,
      destMember.Type);

    return new MappingDescriptor(destMember.Name, mappingExpression);
  }

  private MappingDescriptor? TryCreateMappingWithIncludedMapper(
    MemberInfo sourceMember,
    MemberInfo destMember,
    MapperMethodMetadata methodMetadata)
  {
    // Extract element types if these are collection types
    var sourceElementType = CollectionHelpers.GetCollectionElementType(sourceMember.Type);
    var destElementType = CollectionHelpers.GetCollectionElementType(destMember.Type);

    // Check for collection/non-collection mismatch
    var sourceIsCollection = sourceElementType != null;
    var destIsCollection = destElementType != null;

    if (sourceIsCollection != destIsCollection)
    {
      // One is a collection and the other is not - this is a type mismatch
      return null;
    }

    var isCollectionMapping = sourceIsCollection && destIsCollection;

    // Use element types for collection mapping, otherwise use the member types directly
    var sourceTypeToMatch = sourceElementType ?? sourceMember.Type;
    var destTypeToMatch = destElementType ?? destMember.Type;

    // Look through included mappers to find one that can map from source type to destination type
    foreach (var includedMapper in methodMetadata.IncludedMappers)
    {
      // Find the ToDto (or similar) method in the included mapper
      var mapperMethods = includedMapper.MapperType.GetMembers()
        .OfType<IMethodSymbol>()
        .Where(m => m.IsPartialDefinition && m.MethodKind == MethodKind.Ordinary)
        .ToList();

      foreach (var mapperMethod in mapperMethods)
      {
        // Check if the mapper method returns the destination type and takes the source type as first parameter
        var returnsDestType = SymbolEqualityComparer.Default.Equals(mapperMethod.ReturnType, destTypeToMatch);

        if (!returnsDestType || mapperMethod.Parameters.Length == 0)
        {
          continue;
        }

        var firstParamMatchesSourceType = SymbolEqualityComparer.Default.Equals(
          mapperMethod.Parameters[0].Type,
          sourceTypeToMatch);

        if (!firstParamMatchesSourceType)
        {
          continue;
        }

        // Found a matching mapper method! Now build the mapping expression
        var mapperFieldName = includedMapper.FieldName;
        var methodName = mapperMethod.Name;
        var sourceExpression = $"{methodMetadata.SourceObjectParameter.Name}.{sourceMember.Name}";

        if (isCollectionMapping)
        {
          // For collections, generate: source.Property.Select(item => _mapper.Method(item, ...)).ToList() or .ToArray()
          var itemParamName = CollectionHelpers.GetItemParameterName(sourceTypeToMatch);

          // Build the parameters for the mapper method call
          var methodParams = new List<string> { itemParamName };

          // Add additional parameters if the mapper method requires them (from the method metadata parameters)
          for (var i = 1; i < mapperMethod.Parameters.Length && i < methodMetadata.Parameters.Count; i++)
          {
            methodParams.Add(methodMetadata.Parameters[i].Name);
          }

          var itemTransformExpression = $"{mapperFieldName}.{methodName}({string.Join(", ", methodParams)})";
          var mappingExpression = CollectionHelpers.BuildCollectionMappingExpression(
            sourceExpression,
            itemParamName,
            itemTransformExpression,
            destMember.Type);

          return new MappingDescriptor(destMember.Name, mappingExpression);
        }
        else
        {
          // For direct mapping, generate: _mapper.Method(source.Property, ...)
          var methodParams = new List<string> { sourceExpression };

          // Add additional parameters if the mapper method requires them (from the method metadata parameters)
          for (var i = 1; i < mapperMethod.Parameters.Length && i < methodMetadata.Parameters.Count; i++)
          {
            methodParams.Add(methodMetadata.Parameters[i].Name);
          }

          var mappingExpression = $"{mapperFieldName}.{methodName}({string.Join(", ", methodParams)})";
          return new MappingDescriptor(destMember.Name, mappingExpression);
        }
      }
    }

    return null;
  }


  private MappingDescriptor CreateSuccessfulMapping(
    MemberInfo sourceMember,
    MemberInfo destMember,
    MapperMethodMetadata methodMetadata)
  {
    var sourceExpression = $"{methodMetadata.SourceObjectParameter.Name}.{sourceMember.Name}";
    return new MappingDescriptor(destMember.Name, sourceExpression);
  }

  private DiagnosedPropertyDescriptor CreateNullableMismatchDiagnostic(
    MemberInfo sourceMember,
    MemberInfo destMember,
    MapperMethodMetadata methodMetadata)
  {
    var memberType = destMember.IsField ? "field" : "property";

    var diagnostic = MapperDiagnostic.NullableToNonNullableMismatch(
      methodMetadata.MethodSymbol.Locations.FirstOrDefault(),
      methodMetadata.ReturnTypeName,
      destMember.Name,
      destMember.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
      methodMetadata.SourceObjectParameter.Symbol.Type.Name,
      sourceMember.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
      MappingConfigurationMethods.MapMemberMethodName,
      memberType);

    methodMetadata.AddDiagnostic(diagnostic);
    return new DiagnosedPropertyDescriptor(destMember.Name);
  }

  private DiagnosedPropertyDescriptor CreateTypeMismatchDiagnostic(
    MemberInfo sourceMember,
    MemberInfo destMember,
    MapperMethodMetadata methodMetadata)
  {
    var memberType = destMember.IsField ? "field" : "property";

    var diagnostic = MapperDiagnostic.TypeMismatchInDirectMapping(
      methodMetadata.MethodSymbol.Locations.FirstOrDefault(),
      methodMetadata.ReturnTypeName,
      destMember.Name,
      destMember.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
      methodMetadata.SourceObjectParameter.Symbol.Type.Name,
      sourceMember.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
      MappingConfigurationMethods.MapMemberMethodName,
      memberType);

    methodMetadata.AddDiagnostic(diagnostic);
    return new DiagnosedPropertyDescriptor(destMember.Name);
  }
}
