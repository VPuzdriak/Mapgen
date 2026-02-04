using Microsoft.CodeAnalysis;

namespace Mapgen.Analyzer.Mapper.Diagnostics;

public sealed class MapperDiagnostic
{
  public string Id { get; }
  public string Title { get; }
  public string MessageFormat { get; }
  public DiagnosticSeverity Severity { get; }
  public Location? Location { get; }
  public object[] MessageArgs { get; }

  public MapperDiagnostic(
    string id,
    string title,
    string messageFormat,
    DiagnosticSeverity severity,
    Location? location,
    params object[] messageArgs)
  {
    Id = id;
    Title = title;
    MessageFormat = messageFormat;
    Severity = severity;
    Location = location;
    MessageArgs = messageArgs;
  }

  public static MapperDiagnostic LambdaBlockNotSupported(Location? location, string methodName)
  {
    return new MapperDiagnostic(
      id: DiagnosticIds.LambdaBlockNotSupported,
      title: "Lambda block expressions are not supported",
      messageFormat: "Lambda block expressions are not supported as they are not memory friendly. Use a simple expression or a method reference in {0}().",
      severity: DiagnosticSeverity.Error,
      location: location,
      methodName);
  }

  public static MapperDiagnostic MissingPropertyMapping(
    Location? location,
    string returnTypeName,
    string memberName,
    string sourceTypeName,
    string mapMemberMethodName,
    string ignoreMemberMethodName,
    string memberType)
  {
    return new MapperDiagnostic(
      id: DiagnosticIds.MissingPropertyMapping,
      title: $"Missing {memberType} mapping",
      messageFormat: $"\"{{0}}\" type has \"{{1}}\" {memberType} which does not exist in \"{{2}}\" type. Please, add custom mapping using {{3}}() or ignore this {memberType} explicitly using {{4}}().",
      severity: DiagnosticSeverity.Error,
      location: location,
      returnTypeName,
      memberName,
      sourceTypeName,
      mapMemberMethodName,
      ignoreMemberMethodName);
  }

  public static MapperDiagnostic MultipleMappingMethods(
    Location? location,
    string mapperClassName,
    string methodName)
  {
    return new MapperDiagnostic(
      id: DiagnosticIds.MultipleMappingMethods,
      title: "Multiple mapping methods not supported",
      messageFormat: "\"{0}\" can have only one mapping method. Create a new class and place \"{1}()\" there.",
      severity: DiagnosticSeverity.Error,
      location: location,
      mapperClassName,
      methodName);
  }

  public static MapperDiagnostic TypeMismatchInDirectMapping(
    Location? location,
    string destinationTypeName,
    string memberName,
    string destinationMemberType,
    string sourceTypeName,
    string sourceMemberType,
    string mapMemberMethodName,
    string memberTypeText)
  {
    return new MapperDiagnostic(
      id: DiagnosticIds.TypeMismatchInDirectMapping,
      title: "Type mismatch in direct mapping",
      messageFormat: $"Direct mapping cannot be used because {memberTypeText} \"{{0}}.{{1}}\" is of type \"{{2}}\" and \"{{3}}.{{1}}\" is of type \"{{4}}\". Use {{5}}() to create custom mapping.",
      severity: DiagnosticSeverity.Error,
      location: location,
      destinationTypeName,
      memberName,
      destinationMemberType,
      sourceTypeName,
      sourceMemberType,
      mapMemberMethodName);
  }

  public static MapperDiagnostic NullableToNonNullableMismatch(
    Location? location,
    string destinationTypeName,
    string memberName,
    string destinationMemberType,
    string sourceTypeName,
    string sourceMemberType,
    string mapMemberMethodName,
    string memberTypeText)
  {
    return new MapperDiagnostic(
      id: DiagnosticIds.NullableToNonNullableMismatch,
      title: "Type mismatch in direct mapping",
      messageFormat:
      $"Direct mapping cannot be used because {memberTypeText} \"{{0}}.{{1}}\" is of type \"{{2}}\" and \"{{3}}.{{1}}\" is of type \"{{4}}\". This can cause NullReferenceException at runtime. Use {{5}}() to create custom mapping with explicit null handling.",
      severity: DiagnosticSeverity.Error,
      location: location,
      destinationTypeName,
      memberName,
      destinationMemberType,
      sourceTypeName,
      sourceMemberType,
      mapMemberMethodName);
  }

  public static MapperDiagnostic RequiredMemberCannotBeIgnored(
    Location? location,
    string propertyName)
  {
    return new MapperDiagnostic(
      id: DiagnosticIds.RequiredMemberCannotBeIgnored,
      title: "Required member cannot be ignored",
      messageFormat: "Member \"{0}\" can't be ignored because it has required keyword.",
      severity: DiagnosticSeverity.Error,
      location: location,
      propertyName);
  }

  public static MapperDiagnostic ParameterizedConstructorRequired(
    Location? location,
    string destinationType,
    string constructorSignatures)
  {
    return new MapperDiagnostic(
      id: DiagnosticIds.ParameterizedConstructorRequired,
      title: "Parameterized constructor requires UseConstructor()",
      messageFormat:
      "Cannot generate mapping to \"{0}\". Type has constructor(s) with parameters but no parameterless constructor. Use UseConstructor() to specify how to map constructor parameters.\n\nAvailable constructors:\n{1}",
      severity: DiagnosticSeverity.Error,
      location: location,
      destinationType,
      constructorSignatures);
  }

  public static MapperDiagnostic AmbiguousConstructorSelection(
    Location? location,
    string destinationType,
    string constructorSignatures)
  {
    return new MapperDiagnostic(
      id: DiagnosticIds.AmbiguousConstructorSelection,
      title: "Multiple constructors available - must specify which to use",
      messageFormat:
      "Cannot generate mapping to \"{0}\". Type has multiple constructors. Use UseConstructor() to specify which constructor parameters to use, or UseEmptyConstructor() to use the parameterless constructor.\n\nAvailable constructors:\n{1}",
      severity: DiagnosticSeverity.Error,
      location: location,
      destinationType,
      constructorSignatures);
  }

  public static MapperDiagnostic UseEmptyConstructorNotPossible(
    Location? location,
    string destinationType,
    string constructorSignatures)
  {
    return new MapperDiagnostic(
      id: DiagnosticIds.UseEmptyConstructorNotPossible,
      title: "UseEmptyConstructor() requires parameterless constructor",
      messageFormat:
      "Cannot use UseEmptyConstructor() for type \"{0}\" because it has no parameterless constructor. Use UseConstructor() instead to specify constructor parameters.\n\nAvailable constructors:\n{1}",
      severity: DiagnosticSeverity.Error,
      location: location,
      destinationType,
      constructorSignatures);
  }

  public static MapperDiagnostic MapperConstructorWithParameters(
    Location? location,
    string mapperClassName)
  {
    return new MapperDiagnostic(
      id: DiagnosticIds.MapperConstructorWithParameters,
      title: "Mapper constructor cannot have parameters",
      messageFormat:
      "Mapper class \"{0}\" constructor cannot have parameters. Mapper constructors should be parameterless and only contain configuration method calls",
      severity: DiagnosticSeverity.Error,
      location: location,
      mapperClassName);
  }

  public static MapperDiagnostic InvalidConstructorStatement(Location? location, string mapperClassName)
  {
    return new MapperDiagnostic(
      id: DiagnosticIds.InvalidConstructorStatement,
      title: "Invalid statement in mapper constructor",
      messageFormat:
      "Mapper class \"{0}\" constructor can only contain calls to mapping configuration methods. Variable declarations, branches, and other statements are not allowed.",
      severity: DiagnosticSeverity.Error,
      location: location,
      mapperClassName);
  }
}
