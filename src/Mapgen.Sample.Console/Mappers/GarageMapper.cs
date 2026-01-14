using Mapgen.Analyzer.Abstractions;
using Mapgen.Sample.Console.Models;

namespace Mapgen.Sample.Console.Mappers;

[Mapper]
internal partial class GarageMapper
{
  internal partial GarageDto ToGarageDto(Garage source, Driver driver);

  public GarageMapper()
  {
    MapCollection<CarDto, Car>(dest => dest.Cars, (car, _, driver) => car.ToCarDto(driver));
  }
}
