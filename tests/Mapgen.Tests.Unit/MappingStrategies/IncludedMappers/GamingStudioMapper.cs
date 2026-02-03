using Mapgen.Analyzer;
using Mapgen.Tests.Unit.MappingStrategies.IncludedMappers.Models;

namespace Mapgen.Tests.Unit.MappingStrategies.IncludedMappers;

[Mapper]
public partial class GamingStudioMapper
{
  public partial GamingStudioDto ToDto(GamingStudio source);

  public GamingStudioMapper()
  {
    IncludeMappers([new PublisherMapper()]);
  }
}
