using Mapgen.Analyzer;
using Mapgen.Sample.Console.Models;

namespace Mapgen.Sample.Console.Mappers;

[Mapper]
public partial class CarMapper
{
  public partial CarDto ToCarDto(Car source, Driver driver);

  public CarMapper()
  {
    IncludeMappers([new CarOwnerMapper()]);
    MapMember(dest => dest.CountryOfOrigin, GetCountryName);
    MapMember(dest => dest.MainDriver, (_, drv) => drv.ToDriverDto());
  }

  private static string GetCountryName(Car src) =>
    src.Make switch
    {
      "Toyota" => "Japan",
      "Ford" => "USA",
      "BMW" => "Germany",
      _ => "Unknown"
    };
}
