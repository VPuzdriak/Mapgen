namespace Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.RecordToRecord.Models;

/// <summary>
/// Simple source record with only positional parameters
/// </summary>
public record AddressRecord(string Street, string City, string ZipCode);
