namespace Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.RecordToRecord.Models;

/// <summary>
/// Source record with positional parameters and properties
/// </summary>
public record PersonRecord(string FirstName, string LastName, int Age)
{
  public string Email { get; init; } = string.Empty;
  public string Phone { get; init; } = string.Empty;
}
