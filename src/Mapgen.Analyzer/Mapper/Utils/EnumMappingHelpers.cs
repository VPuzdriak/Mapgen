using System.Collections.Generic;

using Mapgen.Analyzer.Mapper.Metadata;

using Microsoft.CodeAnalysis;

namespace Mapgen.Analyzer.Mapper.Utils;

/// <summary>
/// Helper class for enum mapping operations.
/// Provides reusable functionality for generating enum switch expressions and managing enum namespaces.
/// </summary>
internal static class EnumMappingHelpers
{
  /// <summary>
  /// Registers an enum mapping method for two enum types.
  /// Handles adding namespaces, checking nullability, and registering the mapping method.
  /// This consolidates the common logic used by multiple mapping strategies.
  /// </summary>
  /// <param name="sourceEnumType">Source enum type (can be nullable)</param>
  /// <param name="destEnumType">Destination enum type (can be nullable)</param>
  /// <param name="methodMetadata">Metadata for the mapper method</param>
  public static void RegisterEnumMapping(
    ITypeSymbol sourceEnumType,
    ITypeSymbol destEnumType,
    MapperMethodMetadata methodMetadata)
  {
    // Add required namespaces for both source and destination enums
    AddEnumNamespace(sourceEnumType, methodMetadata);
    AddEnumNamespace(destEnumType, methodMetadata);

    // Register the enum mapping method
    var isSourceNullable = IsNullableType(sourceEnumType, out var sourceUnderlyingType);
    var isDestNullable = IsNullableType(destEnumType, out var destUnderlyingType);

    methodMetadata.RegisterEnumMappingMethod(
      sourceUnderlyingType,
      destUnderlyingType,
      isSourceNullable,
      isDestNullable);
  }

  /// <summary>
  /// Generates a method call expression for enum mapping.
  /// Registers a static method that will be generated to handle the enum conversion.
  /// </summary>
  /// <param name="sourceEnumType">Source enum type (can be nullable)</param>
  /// <param name="destEnumType">Destination enum type (can be nullable)</param>
  /// <param name="sourceExpression">Expression for the source value</param>
  /// <param name="methodMetadata">Metadata for the mapper method</param>
  /// <returns>Method call expression</returns>
  public static string GenerateEnumMappingExpression(
    ITypeSymbol sourceEnumType,
    ITypeSymbol destEnumType,
    string sourceExpression,
    MapperMethodMetadata methodMetadata)
  {
    // Register the enum mapping (adds namespaces and registers the method)
    RegisterEnumMapping(sourceEnumType, destEnumType, methodMetadata);

    // Get the method name for the registered mapping
    var isSourceNullable = IsNullableType(sourceEnumType, out var sourceEnum);
    var isDestNullable = IsNullableType(destEnumType, out var destEnum);

    var methodName = methodMetadata.RegisterEnumMappingMethod(
      sourceEnum,
      destEnum,
      isSourceNullable,
      isDestNullable);

    // Return a simple method call expression
    return $"{methodName}({sourceExpression})";
  }

  /// <summary>
  /// Generates the body of a static enum mapping method.
  /// Creates a switch expression that maps enum values by name (not by numeric value).
  /// </summary>
  /// <param name="methodInfo">Metadata about the enum mapping method to generate</param>
  /// <returns>Complete switch expression code for the method body</returns>
  public static string GenerateEnumMappingMethodBody(EnumMappingMethodInfo methodInfo)
  {
    var sourceEnum = methodInfo.SourceEnumType;
    var destEnum = methodInfo.DestEnumType;
    var isSourceNullable = methodInfo.IsSourceNullable;
    var isDestNullable = methodInfo.IsDestNullable;

    // Get enum member names
    var sourceMembers = TypeCompatibilityChecker.GetEnumMemberNames(sourceEnum);
    var destMembers = TypeCompatibilityChecker.GetEnumMemberNames(destEnum);

    // Determine if we need fully qualified names (when enum names conflict)
    var hasNameConflict = HasEnumNameConflict(sourceEnum, destEnum);

    var sourceTypeName = GetEnumTypeName(sourceEnum, hasNameConflict);
    var destTypeName = GetEnumTypeName(destEnum, hasNameConflict);

    // Build switch expression cases
    var switchCases = BuildEnumSwitchCases(
      sourceMembers,
      destMembers,
      sourceTypeName,
      destTypeName,
      isSourceNullable,
      isDestNullable,
      "value");

    // Format switch expression with proper indentation for method body
    return FormatSwitchExpressionForMethod(switchCases);
  }

  /// <summary>
  /// Adds the namespace of an enum type to the required usings for the generated mapper.
  /// Handles nullable enum types by unwrapping them first.
  /// </summary>
  /// <param name="enumType">The enum type (can be nullable)</param>
  /// <param name="methodMetadata">Metadata for the mapper method</param>
  private static void AddEnumNamespace(ITypeSymbol enumType, MapperMethodMetadata methodMetadata)
  {
    // Unwrap nullable to get actual enum type
    var actualEnumType = UnwrapNullableType(enumType);

    // Add the enum's containing namespace to required usings
    if (actualEnumType.ContainingNamespace is { IsGlobalNamespace: false })
    {
      var namespaceName = actualEnumType.ContainingNamespace.ToDisplayString();
      methodMetadata.AddRequiredUsing(namespaceName);
    }
  }

  /// <summary>
  /// Unwraps a nullable type to get the underlying type.
  /// </summary>
  private static ITypeSymbol UnwrapNullableType(ITypeSymbol type)
  {
    if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
    {
      return nullable.TypeArguments[0];
    }

    return type;
  }

  /// <summary>
  /// Checks if a type is nullable and returns the underlying type.
  /// </summary>
  /// <param name="type">Type to check</param>
  /// <param name="underlyingType">The underlying type (set if nullable)</param>
  /// <returns>True if the type is nullable, false otherwise</returns>
  private static bool IsNullableType(ITypeSymbol type, out ITypeSymbol underlyingType)
  {
    if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
    {
      underlyingType = nullable.TypeArguments[0];
      return true;
    }

    underlyingType = type;
    return false;
  }

  /// <summary>
  /// Checks if two enum types have the same simple name but different namespaces,
  /// which would cause ambiguity if both namespaces are in using statements.
  /// </summary>
  private static bool HasEnumNameConflict(ITypeSymbol sourceEnum, ITypeSymbol destEnum) =>
    sourceEnum.Name == destEnum.Name;

  /// <summary>
  /// Gets the type name for an enum, using fully qualified format if needed to avoid ambiguity.
  /// </summary>
  private static string GetEnumTypeName(ITypeSymbol enumType, bool useFullyQualified) =>
    useFullyQualified
      ? enumType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)
      : enumType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

  /// <summary>
  /// Builds the list of switch case expressions for enum mapping.
  /// </summary>
  private static List<string> BuildEnumSwitchCases(
    List<string> sourceMembers,
    List<string> destMembers,
    string sourceTypeName,
    string destTypeName,
    bool isSourceNullable,
    bool isDestNullable,
    string sourceExpression)
  {
    var switchCases = new List<string>();

    // Add cases for each matching enum member
    foreach (var memberName in sourceMembers)
    {
      if (destMembers.Contains(memberName))
      {
        switchCases.Add($"{sourceTypeName}.{memberName} => {destTypeName}.{memberName}");
      }
    }

    // Handle nullable cases
    if (isSourceNullable && isDestNullable)
    {
      switchCases.Add("null => null");
    }

    // Add default case
    switchCases.Add($"_ => throw new ArgumentOutOfRangeException(nameof({sourceExpression}), {sourceExpression}, \"Unexpected enum value\")");

    return switchCases;
  }

  /// <summary>
  /// Formats the switch expression with proper indentation for method bodies.
  /// Used when generating static enum mapping methods.
  /// </summary>
  private static string FormatSwitchExpressionForMethod(List<string> switchCases)
  {
    var switchBody = string.Join(",\n        ", switchCases);
    return $"value switch\n      {{\n        {switchBody}\n      }}";
  }
}
