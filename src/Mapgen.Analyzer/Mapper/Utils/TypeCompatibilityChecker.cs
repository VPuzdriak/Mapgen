using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Mapgen.Analyzer.Mapper.Utils;

/// <summary>
/// Helper class for checking type compatibility between source and destination types.
/// Provides DRY functionality for type matching and implicit conversion detection.
/// </summary>
internal static class TypeCompatibilityChecker
{
  /// <summary>
  /// Checks if source type can be assigned to destination type.
  /// Handles exact matches (including nullable annotations) and implicit conversions.
  /// </summary>
  /// <param name="sourceType">The source type</param>
  /// <param name="destType">The destination type</param>
  /// <param name="semanticModel">The semantic model for compilation context</param>
  /// <returns>True if types are compatible, false otherwise</returns>
  public static bool AreTypesCompatible(
    ITypeSymbol sourceType,
    ITypeSymbol destType,
    SemanticModel semanticModel)
  {
    // Check for exact match (including nullable annotations)
    if (AreTypesExactMatch(sourceType, destType))
    {
      return true;
    }

    // Check for implicit conversion
    return HasImplicitConversion(sourceType, destType, semanticModel);
  }

  /// <summary>
  /// Checks if two types are an exact match, including nullable annotations.
  /// </summary>
  public static bool AreTypesExactMatch(ITypeSymbol sourceType, ITypeSymbol destType)
  {
    return SymbolEqualityComparer.Default.Equals(sourceType, destType);
  }

  /// <summary>
  /// Checks if types match ignoring nullable annotations.
  /// </summary>
  public static bool AreTypesMatchIgnoringNullability(ITypeSymbol sourceType, ITypeSymbol destType)
  {
    return SymbolEqualityComparer.Default.Equals(
      sourceType.WithNullableAnnotation(NullableAnnotation.None),
      destType.WithNullableAnnotation(NullableAnnotation.None));
  }

  /// <summary>
  /// Checks if there's an implicit conversion from source type to destination type.
  /// Only considers built-in conversions, not user-defined ones.
  /// </summary>
  public static bool HasImplicitConversion(
    ITypeSymbol sourceType,
    ITypeSymbol destType,
    SemanticModel semanticModel)
  {
    if (semanticModel.Compilation is not CSharpCompilation csharpCompilation)
    {
      return false;
    }

    var conversion = csharpCompilation.ClassifyConversion(sourceType, destType);
    return conversion is { IsImplicit: true, IsUserDefined: false };
  }

  /// <summary>
  /// Checks if the nullable annotation mismatch represents a nullable-to-non-nullable scenario.
  /// This is a potential issue that should be reported as a diagnostic.
  /// </summary>
  public static bool IsNullableToNonNullableMismatch(
    ITypeSymbol sourceType,
    ITypeSymbol destType)
  {
    // Types must match ignoring nullability
    if (!AreTypesMatchIgnoringNullability(sourceType, destType))
    {
      return false;
    }

    var sourceIsNullable = sourceType.NullableAnnotation == NullableAnnotation.Annotated;
    var destIsNullable = destType.NullableAnnotation == NullableAnnotation.Annotated;

    return sourceIsNullable && !destIsNullable;
  }
}
