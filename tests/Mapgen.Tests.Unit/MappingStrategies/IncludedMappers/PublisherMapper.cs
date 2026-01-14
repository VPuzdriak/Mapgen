using Mapgen.Analyzer;
using Mapgen.Tests.Unit.MappingStrategies.IncludedMappers.Models;

namespace Mapgen.Tests.Unit.MappingStrategies.IncludedMappers;

[Mapper]
public partial class PublisherMapper
{
  public partial PublisherDto ToDto(Publisher source);
}
