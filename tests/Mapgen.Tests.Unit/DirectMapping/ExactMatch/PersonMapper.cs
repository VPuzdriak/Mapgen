using Mapgen.Analyzer.Abstractions;
using Mapgen.Tests.Unit.DirectMapping.ExactMatch.Models;

namespace Mapgen.Tests.Unit.DirectMapping.ExactMatch;

[Mapper]
public partial class PersonMapper
{
  public partial PersonDto ToDto(Person source);
}
