using Mapgen.Analyzer.Benchmarks.Models;

namespace Mapgen.Analyzer.Benchmarks.Mappers.Mapperly;

[Riok.Mapperly.Abstractions.Mapper]
public partial class SimpleMapper
{
  public partial SimpleDto ToDto(SimpleEntity entity);
}
