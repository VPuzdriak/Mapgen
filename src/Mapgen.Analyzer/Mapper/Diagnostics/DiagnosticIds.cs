namespace Mapgen.Analyzer.Mapper.Diagnostics;

/// <summary>
/// Constants for diagnostic IDs and categories.
/// </summary>
internal static class DiagnosticIds
{
  public const string MissingPropertyMapping = "MAPPER001";
  public const string LambdaBlockNotSupported = "MAPPER002";
  public const string MultipleMappingMethods = "MAPPER003";
  public const string TypeMismatchInDirectMapping = "MAPPER004";
  public const string NullableToNonNullableMismatch = "MAPPER005";
  public const string RequiredMemberCannotBeIgnored = "MAPPER006";
  public const string ParameterizedConstructorRequired = "MAPPER007";
  public const string AmbiguousConstructorSelection = "MAPPER008";
  public const string UseEmptyConstructorNotPossible = "MAPPER009";
  public const string MapperConstructorWithParameters = "MAPPER010";
  public const string InvalidConstructorStatement = "MAPPER011";

  public const string Category = "Mapper";
}
