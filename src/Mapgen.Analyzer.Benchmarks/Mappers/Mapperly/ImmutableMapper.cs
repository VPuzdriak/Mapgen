using Mapgen.Analyzer.Benchmarks.Models;

using Riok.Mapperly.Abstractions;

namespace Mapgen.Analyzer.Benchmarks.Mappers.Mapperly;

[Mapper]
public partial class ImmutableMapper
{
  [MapperIgnoreSource(nameof(ImmutableEntity.DateOfBirth))]
  public partial ImmutableDto ToDto(ImmutableEntity entity);

  // Mapperly will automatically detect and use the best-fit constructor
}
