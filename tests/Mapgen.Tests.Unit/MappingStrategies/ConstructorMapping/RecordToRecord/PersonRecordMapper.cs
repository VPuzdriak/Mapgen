using Mapgen.Analyzer;
using Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.RecordToRecord.Models;

namespace Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.RecordToRecord;

/// <summary>
/// Mapper for record to record with primary constructor parameters + additional properties
/// </summary>
[Mapper]
public partial class RecordToRecordPersonMapper
{
  public partial PersonDto ToDto(PersonRecord source);

  public RecordToRecordPersonMapper()
  {
    UseConstructor(
      source => source.FirstName,
      source => source.LastName,
      source => source.Age,
      source => source.Email
    );
    // Phone should be mapped via object initializer (required property)
  }
}
