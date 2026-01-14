using Mapgen.Analyzer;
using Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.RecordToRecord.Models;

namespace Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.RecordToRecord;

/// <summary>
/// Mapper for simple record to record - all via primary constructor
/// </summary>
[Mapper]
public partial class AddressRecordMapper
{
  public partial AddressDto ToDto(AddressRecord source);

  public AddressRecordMapper()
  {
    UseConstructor(
      source => source.Street,
      source => source.City,
      source => source.ZipCode
    );
  }
}
