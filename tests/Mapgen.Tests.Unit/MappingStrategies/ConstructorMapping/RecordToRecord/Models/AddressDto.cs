namespace Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.RecordToRecord.Models;

/// <summary>
/// Simple destination record - all properties via primary constructor
/// </summary>
public record AddressDto(string Street, string City, string ZipCode);
