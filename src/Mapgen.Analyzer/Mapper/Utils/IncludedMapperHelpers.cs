using System.Collections.Generic;
using System.Linq;

using Mapgen.Analyzer.Mapper.Metadata;

using Microsoft.CodeAnalysis;

namespace Mapgen.Analyzer.Mapper.Utils;

/// <summary>
/// Helper methods for working with included mappers.
/// Provides DRY functionality for finding mapper methods and building mapping expressions.
/// </summary>
internal static class IncludedMapperHelpers
{
  /// <summary>
  /// Finds an included mapper method that can map from source type to destination type.
  /// Handles both direct type mapping and collection element type mapping.
  /// </summary>
  /// <param name="sourceType">The source type to map from</param>
  /// <param name="destType">The destination type to map to</param>
  /// <param name="methodMetadata">The mapper method metadata containing included mappers</param>
  /// <param name="includedMapper">The found included mapper (output)</param>
  /// <param name="mapperMethod">The found mapper method (output)</param>
  /// <param name="isCollectionMapping">Whether this is a collection mapping (output)</param>
  /// <returns>True if a matching mapper method was found, false otherwise</returns>
  public static bool TryFindIncludedMapperMethod(
    ITypeSymbol sourceType,
    ITypeSymbol destType,
    MapperMethodMetadata methodMetadata,
    out IncludedMapperInfo? includedMapper,
    out IMethodSymbol? mapperMethod,
    out bool isCollectionMapping)
  {
    includedMapper = null;
    mapperMethod = null;
    isCollectionMapping = false;

    // Extract element types if these are collection types
    var sourceElementType = CollectionHelpers.GetCollectionElementType(sourceType);
    var destElementType = CollectionHelpers.GetCollectionElementType(destType);

    // Check for collection/non-collection mismatch
    var sourceIsCollection = sourceElementType != null;
    var destIsCollection = destElementType != null;

    if (sourceIsCollection != destIsCollection)
    {
      // One is a collection and the other is not - can't map
      return false;
    }

    isCollectionMapping = sourceIsCollection && destIsCollection;

    // Use element types for collection mapping, otherwise use the types directly
    var sourceTypeToMatch = sourceElementType ?? sourceType;
    var destTypeToMatch = destElementType ?? destType;

    // Look through included mappers to find one that can map from source type to destination type
    foreach (var mapper in methodMetadata.IncludedMappers)
    {
      // Find mapping methods in the included mapper
      var mapperMethods = mapper.MapperType.GetMembers()
        .OfType<IMethodSymbol>()
        .Where(m => m.IsPartialDefinition && m.MethodKind == MethodKind.Ordinary)
        .ToList();

      foreach (var method in mapperMethods)
      {
        // Check if the mapper method returns the destination type and takes the source type as first parameter
        var returnsDestType = SymbolEqualityComparer.Default.Equals(method.ReturnType, destTypeToMatch);

        if (!returnsDestType || method.Parameters.Length == 0)
        {
          continue;
        }

        var firstParamMatchesSourceType = SymbolEqualityComparer.Default.Equals(
          method.Parameters[0].Type,
          sourceTypeToMatch);

        if (firstParamMatchesSourceType)
        {
          // Found a matching mapper method
          includedMapper = mapper;
          mapperMethod = method;
          return true;
        }
      }
    }

    return false;
  }

  /// <summary>
  /// Builds a mapping expression using an included mapper.
  /// Handles both direct mapping and collection mapping scenarios.
  /// </summary>
  /// <param name="sourceExpression">The source expression (e.g., "source.Property")</param>
  /// <param name="sourceType">The source type</param>
  /// <param name="destType">The destination type</param>
  /// <param name="includedMapper">The included mapper to use</param>
  /// <param name="mapperMethod">The mapper method to invoke</param>
  /// <param name="isCollectionMapping">Whether this is a collection mapping</param>
  /// <param name="methodMetadata">The mapper method metadata for additional parameters</param>
  /// <returns>The complete mapping expression</returns>
  public static string BuildMappingExpression(
    string sourceExpression,
    ITypeSymbol sourceType,
    ITypeSymbol destType,
    IncludedMapperInfo includedMapper,
    IMethodSymbol mapperMethod,
    bool isCollectionMapping,
    MapperMethodMetadata methodMetadata)
  {
    var mapperFieldName = includedMapper.FieldName;
    var methodName = mapperMethod.Name;

    if (isCollectionMapping)
    {
      // For collections, generate: source.Property.Select(item => _mapper.Method(item, ...)).ToList()
      var sourceElementType = CollectionHelpers.GetCollectionElementType(sourceType);
      var itemParamName = CollectionHelpers.GetItemParameterName(sourceElementType!);

      // Build the parameters for the mapper method call
      var methodParams = BuildMapperMethodParameters(itemParamName, mapperMethod, methodMetadata);

      var itemTransformExpression = $"{mapperFieldName}.{methodName}({string.Join(", ", methodParams)})";
      return CollectionHelpers.BuildCollectionMappingExpression(
        sourceExpression,
        itemParamName,
        itemTransformExpression,
        destType);
    }
    else
    {
      // For direct mapping, generate: _mapper.Method(source.Property, ...)
      var methodParams = BuildMapperMethodParameters(sourceExpression, mapperMethod, methodMetadata);
      return $"{mapperFieldName}.{methodName}({string.Join(", ", methodParams)})";
    }
  }

  /// <summary>
  /// Builds the parameter list for a mapper method call.
  /// Includes the first parameter (source expression) and additional parameters from method metadata.
  /// </summary>
  private static List<string> BuildMapperMethodParameters(
    string firstParameter,
    IMethodSymbol mapperMethod,
    MapperMethodMetadata methodMetadata)
  {
    var methodParams = new List<string> { firstParameter };

    // Add additional parameters if the mapper method requires them (from the method metadata parameters)
    for (var i = 1; i < mapperMethod.Parameters.Length && i < methodMetadata.Parameters.Count; i++)
    {
      methodParams.Add(methodMetadata.Parameters[i].Name);
    }

    return methodParams;
  }
}
