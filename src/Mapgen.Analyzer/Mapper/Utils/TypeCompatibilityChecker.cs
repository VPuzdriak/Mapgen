using System.Collections.Generic;
using System.Linq;

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
  /// Handles both reference types (string? -> string) and value types (int? -> int, OrderStatus? -> OrderStatusDto).
  /// </summary>
  public static bool IsNullableToNonNullableMismatch(
    ITypeSymbol sourceType,
    ITypeSymbol destType)
  {
    // Check for Nullable<T> value types (int?, OrderStatus?, etc.)
    if (sourceType is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } &&
        destType is not INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T })
    {
      // Source is Nullable<T> but destination is not nullable
      // For enums: OrderStatus? -> OrderStatusDto  
      // For value types: int? -> int
      // Both should be reported as nullable-to-non-nullable mismatch
      return true;
    }

    // Case 2: Reference types with nullable annotation (string? -> string)
    // Types must match ignoring nullability
    if (!AreTypesMatchIgnoringNullability(sourceType, destType))
    {
      return false;
    }

    var sourceIsNullable = sourceType.NullableAnnotation == NullableAnnotation.Annotated;
    var destIsNullable = destType.NullableAnnotation == NullableAnnotation.Annotated;

    return sourceIsNullable && !destIsNullable;
  }

  /// <summary>
  /// Checks if both types are enums and determines their compatibility.
  /// </summary>
  /// <param name="sourceType">Source enum type</param>
  /// <param name="destType">Destination enum type</param>
  /// <param name="missingMembers">Output parameter containing source members not in destination (comma-separated)</param>
  /// <returns>True if enums are compatible (all source members exist in destination), false otherwise</returns>
  public static bool AreEnumsCompatible(
    ITypeSymbol sourceType,
    ITypeSymbol destType,
    out IReadOnlyList<string> missingMembers)
  {
    missingMembers = [];

    // Unwrap nullable enums to their underlying enum types
    var sourceEnumType = sourceType;
    if (sourceType is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T, TypeArguments.Length: 1 } sourceNullable)
    {
      sourceEnumType = sourceNullable.TypeArguments[0];
    }

    var destEnumType = destType;
    if (destType is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T, TypeArguments.Length: 1 } destNullable)
    {
      destEnumType = destNullable.TypeArguments[0];
    }

    // Both must be enums
    if (sourceEnumType.TypeKind != TypeKind.Enum || destEnumType.TypeKind != TypeKind.Enum)
    {
      return false;
    }

    // If they're the same type, they're compatible
    if (AreTypesExactMatch(sourceEnumType, destEnumType))
    {
      return true;
    }

    // Get enum members
    var sourceMembers = GetEnumMemberNames(sourceEnumType);
    var destMembers = GetEnumMemberNames(destEnumType);

    // Check if all source members exist in destination
    missingMembers = sourceMembers.Where(sm => !destMembers.Contains(sm)).ToList();

    if (missingMembers.Any())
    {
      return false;
    }

    return true;
  }

  /// <summary>
  /// Gets the names of all enum members from an enum type.
  /// </summary>
  public static List<string> GetEnumMemberNames(ITypeSymbol enumType)
  {
    if (enumType is not INamedTypeSymbol namedType)
    {
      return new List<string>();
    }

    return namedType.GetMembers()
      .OfType<IFieldSymbol>()
      .Where(f => f.IsConst && f.HasConstantValue)
      .Select(f => f.Name)
      .ToList();
  }
}
