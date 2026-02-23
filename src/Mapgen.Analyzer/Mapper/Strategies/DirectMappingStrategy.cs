using System.Collections.Generic;
using System.Linq;

using Mapgen.Analyzer.Mapper.Diagnostics;
using Mapgen.Analyzer.Mapper.MappingDescriptors;
using Mapgen.Analyzer.Mapper.Metadata;
using Mapgen.Analyzer.Mapper.Utils;

using Microsoft.CodeAnalysis;

namespace Mapgen.Analyzer.Mapper.Strategies;

public sealed class DirectMappingStrategy(SemanticModel semanticModel)
{
  public BaseMappingDescriptor TryCreateDirectMapping(
    MemberInfo sourceMember,
    MemberInfo destMember,
    MapperMethodMetadata methodMetadata)
  {
    // Check if types are compatible (exact match including nullable annotations or implicit conversion exists)
    if (TypeCompatibilityChecker.AreTypesExactMatch(sourceMember.Type, destMember.Type))
    {
      // Exact match - create direct mapping
      return CreateSuccessfulMapping(sourceMember, destMember, methodMetadata);
    }

    // Check for nullable-to-non-nullable mismatch BEFORE enum check
    // This handles both value types (int? -> int) and enums (OrderStatus? -> OrderStatusDto)
    if (TypeCompatibilityChecker.IsNullableToNonNullableMismatch(sourceMember.Type, destMember.Type))
    {
      return CreateNullableMismatchDiagnostic(sourceMember, destMember, methodMetadata);
    }

    // Check if both are enums with compatible members
    if (TypeCompatibilityChecker.AreEnumsCompatible(sourceMember.Type, destMember.Type, out var missingMembers))
    {
      // Enums are compatible - create mapping with cast
      return CreateEnumMapping(sourceMember, destMember, methodMetadata);
    }

    // If enums but incompatible, report specific enum diagnostic
    if (missingMembers.Any())
    {
      return CreateIncompatibleEnumDiagnostic(sourceMember, destMember, missingMembers, methodMetadata);
    }


    // Check if there's an implicit conversion from source to destination type (for different types like short -> int)
    if (TypeCompatibilityChecker.HasImplicitConversion(sourceMember.Type, destMember.Type, semanticModel))
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
    var elementTypesMatch = TypeCompatibilityChecker.AreTypesExactMatch(sourceElementType, destElementType);
    var hasImplicitConversion = !elementTypesMatch &&
                                TypeCompatibilityChecker.HasImplicitConversion(sourceElementType, destElementType, semanticModel);

    // Check if element types are compatible enums (same member names)
    var areEnumsCompatible = TypeCompatibilityChecker.AreEnumsCompatible(sourceElementType, destElementType, out _);

    if (!elementTypesMatch && !hasImplicitConversion && !areEnumsCompatible)
    {
      // Element types are not compatible
      return null;
    }

    // Element types are compatible - generate collection mapping
    var sourceExpression = $"{methodMetadata.SourceObjectParameter.Name}.{sourceMember.Name}";
    var itemParamName = CollectionHelpers.GetItemParameterName(sourceElementType);

    // Determine the item transformation expression
    string itemTransformExpression;

    if (areEnumsCompatible)
    {
      // For enums, generate method call expression for mapping
      itemTransformExpression = EnumMappingHelpers.GenerateEnumMappingExpression(
        sourceElementType,
        destElementType,
        itemParamName,
        methodMetadata);
    }
    else if (hasImplicitConversion)
    {
      // When element types differ (even with implicit conversion), we need explicit cast
      // because generic interfaces are invariant in C#.
      // Example: List<short> -> IImmutableList<long>
      // Without cast: Select(x => x).ToImmutableList() returns ImmutableList<short>
      // With cast: Select(x => (long)x).ToImmutableList() returns ImmutableList<long>
      itemTransformExpression = $"({destElementType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}){itemParamName}";
    }
    else
    {
      // Exact match - no transformation needed
      itemTransformExpression = itemParamName;
    }

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
    // Try to find an included mapper method that can handle this mapping
    if (!IncludedMapperHelpers.TryFindIncludedMapperMethod(
          sourceMember.Type,
          destMember.Type,
          methodMetadata,
          out var includedMapper,
          out var mapperMethod,
          out var isCollectionMapping))
    {
      return null;
    }

    // Build the mapping expression using the found mapper
    var sourceExpression = $"{methodMetadata.SourceObjectParameter.Name}.{sourceMember.Name}";
    var mappingExpression = IncludedMapperHelpers.BuildMappingExpression(
      sourceExpression,
      sourceMember.Type,
      destMember.Type,
      includedMapper!,
      mapperMethod!,
      isCollectionMapping,
      methodMetadata);

    return new MappingDescriptor(destMember.Name, mappingExpression);
  }


  private MappingDescriptor CreateSuccessfulMapping(
    MemberInfo sourceMember,
    MemberInfo destMember,
    MapperMethodMetadata methodMetadata)
  {
    var sourceExpression = $"{methodMetadata.SourceObjectParameter.Name}.{sourceMember.Name}";
    return new MappingDescriptor(destMember.Name, sourceExpression);
  }

  private MappingDescriptor CreateEnumMapping(
    MemberInfo sourceMember,
    MemberInfo destMember,
    MapperMethodMetadata methodMetadata)
  {
    var sourceExpression = $"{methodMetadata.SourceObjectParameter.Name}.{sourceMember.Name}";


    // Generate method call expression for enum mapping
    // This will register the method and return a call expression like "MapOrderStatusToOrderStatusDto(order.Status)"
    var mappingExpression = EnumMappingHelpers.GenerateEnumMappingExpression(
      sourceMember.Type,
      destMember.Type,
      sourceExpression,
      methodMetadata);

    return new MappingDescriptor(destMember.Name, mappingExpression);
  }


  private DiagnosedPropertyDescriptor CreateIncompatibleEnumDiagnostic(
    MemberInfo sourceMember,
    MemberInfo destMember,
    IReadOnlyList<string> missingMembers,
    MapperMethodMetadata methodMetadata)
  {
    var memberType = destMember.IsField ? "field" : "property";

    var diagnostic = MapperDiagnostic.IncompatibleEnumMapping(
      methodMetadata.MethodSymbol.Locations.FirstOrDefault(),
      methodMetadata.ReturnTypeName,
      destMember.Name,
      destMember.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
      methodMetadata.SourceObjectParameter.Symbol.Type.Name,
      sourceMember.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
      missingMembers,
      MappingConfigurationMethods.MapMemberMethodName,
      memberType);

    methodMetadata.AddDiagnostic(diagnostic);
    return new DiagnosedPropertyDescriptor(destMember.Name);
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
