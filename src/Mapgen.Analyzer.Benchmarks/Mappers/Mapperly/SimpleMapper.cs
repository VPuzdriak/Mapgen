using Mapgen.Analyzer.Benchmarks.Models;

namespace Mapgen.Analyzer.Benchmarks.Mappers.Mapperly;

[Mapper]
public partial class SimpleMapper
{
  public partial SimpleDto ToDto(SimpleEntity entity);
}
