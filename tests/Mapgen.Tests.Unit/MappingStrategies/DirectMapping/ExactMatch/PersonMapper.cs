using Mapgen.Analyzer;
using Mapgen.Tests.Unit.MappingStrategies.DirectMapping.ExactMatch.Models;

namespace Mapgen.Tests.Unit.MappingStrategies.DirectMapping.ExactMatch;

[Mapper]
public partial class PersonMapper
{
  public partial PersonDto ToDto(Person source);
}
