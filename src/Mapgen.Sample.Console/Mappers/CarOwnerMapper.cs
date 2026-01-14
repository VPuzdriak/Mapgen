using Mapgen.Analyzer;
using Mapgen.Sample.Console.Models;

namespace Mapgen.Sample.Console.Mappers;

[Mapper]
public partial class CarOwnerMapper
{
  public partial CarOwnerDto ToCarOwnerDto(CarOwner source);

  public CarOwnerMapper()
  {
    MapMember(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}");
  }
}
