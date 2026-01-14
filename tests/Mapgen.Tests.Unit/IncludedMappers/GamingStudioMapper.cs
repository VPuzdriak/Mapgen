using Mapgen.Analyzer.Abstractions;
using Mapgen.Tests.Unit.IncludedMappers.Models;

namespace Mapgen.Tests.Unit.IncludedMappers;

[Mapper]
public partial class GamingStudioMapper
{
  public partial GamingStudioDto ToDto(GamingStudio source);

  public GamingStudioMapper()
  {
    IncludeMappers([
      new PublisherMapper()
    ]);
  }
}
