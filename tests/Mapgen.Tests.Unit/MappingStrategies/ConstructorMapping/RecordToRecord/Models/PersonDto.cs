namespace Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.RecordToRecord.Models;

/// <summary>
/// Destination record with readonly properties via primary constructor
/// </summary>
public record PersonDto(string FirstName, string LastName, int Age, string Email)
{
  public string FullName => $"{FirstName} {LastName}";
  public required string Phone { get; init; }
}
