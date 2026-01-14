using Mapgen.Analyzer.Benchmarks.Models;

namespace Mapgen.Analyzer.Benchmarks.Mappers.Mapgen;

[Mapper]
public partial class SimpleMapgenMapper
{
  public partial SimpleDto ToDto(SimpleEntity entity);
}
