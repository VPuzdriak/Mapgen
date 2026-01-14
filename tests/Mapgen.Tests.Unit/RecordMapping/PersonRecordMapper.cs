using Mapgen.Analyzer;
using Mapgen.Tests.Unit.RecordMapping.Models;

namespace Mapgen.Tests.Unit.RecordMapping;

[Mapper]
public partial class PersonRecordMapper
{
  public partial PersonRecordDto ToDto(PersonRecord source);
}

