using Mapgen.Analyzer;
using Mapgen.Sample.Console.Models;

namespace Mapgen.Sample.Console.Mappers;

[Mapper]
public partial class DriverMapper
{
  public partial DriverDto ToDriverDto(Driver source);

  public DriverMapper()
  {
    MapMember(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}");
    IgnoreMember(dest => dest.ExperienceInYears);
  }
}
