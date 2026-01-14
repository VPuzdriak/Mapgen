using Mapgen.Analyzer;
using Mapgen.Sample.Console.Models;

namespace Mapgen.Sample.Console.Mappers;

[Mapper]
internal partial class GarageMapper
{
  internal partial GarageDto ToGarageDto(Garage garage, Driver driver);

  public GarageMapper()
  {
    IncludeMappers([new CarMapper()]);
  }
}
