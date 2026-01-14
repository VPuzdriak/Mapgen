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

  public static MapperDiagnostic LambdaBlockNotSupported(Location? location, string propertyName, string methodName)
  {
    return new MapperDiagnostic(
      id: "MAPPER002",
      title: "Lambda block expressions are not supported",
      messageFormat: "Lambda block expressions are not supported as they are not memory friendly. Use a simple expression or a method reference in {1}().",
      severity: DiagnosticSeverity.Error,
      location: location,
      propertyName,
      methodName);
  }

  public static MapperDiagnostic MissingPropertyMapping(
    Location? location,
    string returnTypeName,
    string propertyName,
    string sourceTypeName,
    string mapMemberMethodName,
    string ignoreMemberMethodName)
  {
    return new MapperDiagnostic(
      id: "MAPPER001",
      title: "Missing property mapping",
      messageFormat: "{0} type has {1} property which does not exist in {2} type. Please, add custom mapping using {3}() or ignore this property explicitly using {4}().",
      severity: DiagnosticSeverity.Error,
      location: location,
      returnTypeName,
      propertyName,
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
      id: "MAPPER003",
      title: "Multiple mapping methods not supported",
      messageFormat: "{0} can have only one mapping method. Create a new class and place {1} there.",
      severity: DiagnosticSeverity.Error,
      location: location,
      mapperClassName,
      methodName);
  }

  public static MapperDiagnostic TypeMismatchInDirectMapping(
    Location? location,
    string destinationTypeName,
    string propertyName,
    string destinationPropertyType,
    string sourceTypeName,
    string sourcePropertyType,
    string mapMemberMethodName)
  {
    return new MapperDiagnostic(
      id: "MAPPER004",
      title: "Type mismatch in direct mapping",
      messageFormat: "Direct mapping cannot be used because property {0}.{1} is of type {2} and {3}.{1} is of type {4}. Use {5}() to create custom mapping.",
      severity: DiagnosticSeverity.Error,
      location: location,
      destinationTypeName,
      propertyName,
      destinationPropertyType,
      sourceTypeName,
      sourcePropertyType,
      mapMemberMethodName);
  }

  public static MapperDiagnostic NullableToNonNullableMismatch(
    Location? location,
    string destinationTypeName,
    string propertyName,
    string destinationPropertyType,
    string sourceTypeName,
    string sourcePropertyType,
    string mapMemberMethodName)
  {
    return new MapperDiagnostic(
      id: "MAPPER005",
      title: "Type mismatch in direct mapping",
      messageFormat: "Direct mapping cannot be used because property {0}.{1} is of type {2} and {3}.{1} is of type {4}. This can cause NullReferenceException at runtime. Use {5}() to create custom mapping with explicit null handling.",
      severity: DiagnosticSeverity.Error,
      location: location,
      destinationTypeName,
      propertyName,
      destinationPropertyType,
      sourceTypeName,
      sourcePropertyType,
      mapMemberMethodName);
  }

  public static MapperDiagnostic RequiredMemberCannotBeIgnored(
    Location? location,
    string propertyName)
  {
    return new MapperDiagnostic(
      id: "MAPPER006",
      title: "Required member cannot be ignored",
      messageFormat: "Member '{0}' can't be ignored because it has required keyword.",
      severity: DiagnosticSeverity.Error,
      location: location,
      propertyName);
  }
}
