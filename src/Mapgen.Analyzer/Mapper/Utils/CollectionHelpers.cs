using Microsoft.CodeAnalysis;

namespace Mapgen.Analyzer.Mapper.Utils;

/// <summary>
/// Helper methods for collection type operations.
/// </summary>
internal static class CollectionHelpers
{
  /// <summary>
  /// Determines the appropriate collection conversion method (ToList, ToArray, ToHashSet, etc.)
  /// based on the destination collection type.
  /// </summary>
  public static string GetCollectionConversionMethod(ITypeSymbol typeSymbol)
  {
    // If destination is an array, use ToArray()
    if (typeSymbol is IArrayTypeSymbol)
    {
      return "ToArray";
    }

    // If destination is a generic collection type, determine the appropriate method
    if (typeSymbol is INamedTypeSymbol namedType)
    {
      var typeDefinition = namedType.OriginalDefinition;
      var typeName = typeDefinition.ToDisplayString();

      // For HashSet<T>, use ToHashSet()
      if (typeName.StartsWith("System.Collections.Generic.HashSet<"))
      {
        return "ToHashSet";
      }

      // For immutable collections, use the appropriate ToImmutableXxx() method
      if (typeName.StartsWith("System.Collections.Immutable.ImmutableArray<"))
      {
        return "ToImmutableArray";
      }

      if (typeName.StartsWith("System.Collections.Immutable.IImmutableList<") ||
          typeName.StartsWith("System.Collections.Immutable.ImmutableList<"))
      {
        return "ToImmutableList";
      }

      if (typeName.StartsWith("System.Collections.Immutable.IImmutableSet<") ||
          typeName.StartsWith("System.Collections.Immutable.ImmutableHashSet<"))
      {
        return "ToImmutableHashSet";
      }

      // For List<T>, IList<T>, ICollection<T>, IEnumerable<T>, IReadOnlyList<T>, IReadOnlyCollection<T>, use ToList()
      if (typeName.StartsWith("System.Collections.Generic.List<") ||
          typeName.StartsWith("System.Collections.Generic.IList<") ||
          typeName.StartsWith("System.Collections.Generic.ICollection<") ||
          typeName.StartsWith("System.Collections.Generic.IReadOnlyList<") ||
          typeName.StartsWith("System.Collections.Generic.IReadOnlyCollection<") ||
          typeName.StartsWith("System.Collections.Generic.IEnumerable<"))
      {
        return "ToList";
      }
    }

    // Default to ToList()
    return "ToList";
  }

  /// <summary>
  /// Extracts the element type from a collection type (array or generic collection).
  /// Returns null if the type is not a collection.
  /// </summary>
  public static ITypeSymbol? GetCollectionElementType(ITypeSymbol typeSymbol)
  {
    // Check if this is an array type like Car[]
    if (typeSymbol is IArrayTypeSymbol arrayType)
    {
      return arrayType.ElementType;
    }

    // Check if this is a generic collection type like List<T>, IEnumerable<T>, etc.
    if (typeSymbol is INamedTypeSymbol { IsGenericType: true } namedType)
    {
      // Check if it's a collection type (List, IEnumerable, ICollection, etc.)
      var typeDefinition = namedType.OriginalDefinition;
      var typeName = typeDefinition.ToDisplayString();

      if (typeName.StartsWith("System.Collections.Generic.List<") ||
          typeName.StartsWith("System.Collections.Generic.IEnumerable<") ||
          typeName.StartsWith("System.Collections.Generic.ICollection<") ||
          typeName.StartsWith("System.Collections.Generic.IList<") ||
          typeName.StartsWith("System.Collections.Generic.IReadOnlyList<") ||
          typeName.StartsWith("System.Collections.Generic.IReadOnlyCollection<") ||
          typeName.StartsWith("System.Collections.Generic.HashSet<") ||
          typeName.StartsWith("System.Collections.Immutable.IImmutableList<") ||
          typeName.StartsWith("System.Collections.Immutable.ImmutableList<") ||
          typeName.StartsWith("System.Collections.Immutable.ImmutableArray<") ||
          typeName.StartsWith("System.Collections.Immutable.IImmutableSet<") ||
          typeName.StartsWith("System.Collections.Immutable.ImmutableHashSet<"))
      {
        // Return the first generic type argument (the element type)
        return namedType.TypeArguments.Length > 0 ? namedType.TypeArguments[0] : null;
      }
    }

    return null;
  }

  /// <summary>
  /// Generates a lowercase parameter name from a type name.
  /// For example, "Car" becomes "car".
  /// </summary>
  public static string GetItemParameterName(ITypeSymbol typeSymbol)
  {
    var typeName = typeSymbol.Name;
    return char.ToLowerInvariant(typeName[0]) + typeName.Substring(1);
  }

  /// <summary>
  /// Builds a collection mapping expression using Select and the appropriate conversion method.
  /// </summary>
  /// <param name="sourceCollectionExpression">The source collection expression (e.g., "car.Drivers")</param>
  /// <param name="itemParameterName">The item parameter name for the Select lambda (e.g., "driver")</param>
  /// <param name="itemTransformExpression">The transformation expression for each item (e.g., "_mapper.ToDto(driver, garage)")</param>
  /// <param name="destinationCollectionType">The destination collection type to determine conversion method</param>
  /// <returns>Complete collection mapping expression (e.g., "car.Drivers.Select(driver => _mapper.ToDto(driver, garage)).ToList()")</returns>
  public static string BuildCollectionMappingExpression(
    string sourceCollectionExpression,
    string itemParameterName,
    string itemTransformExpression,
    ITypeSymbol destinationCollectionType)
  {
    var conversionMethod = GetCollectionConversionMethod(destinationCollectionType);
    return $"{sourceCollectionExpression}.Select({itemParameterName} => {itemTransformExpression}).{conversionMethod}()";
  }
}
