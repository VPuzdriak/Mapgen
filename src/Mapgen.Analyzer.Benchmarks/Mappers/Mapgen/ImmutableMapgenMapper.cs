using Mapgen.Analyzer.Benchmarks.Models;

namespace Mapgen.Analyzer.Benchmarks.Mappers.Mapgen;

[Mapper]
public partial class ImmutableMapgenMapper
{
  public partial ImmutableDto ToDto(ImmutableEntity entity);

  // Constructor automatically detected and used by Mapgen
}
