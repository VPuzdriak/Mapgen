using Mapgen.Analyzer;
using Mapgen.Tests.Unit.Inheritance.Models;

namespace Mapgen.Tests.Unit.Inheritance;

[Mapper]
public partial class CarMapper
{
  public partial CarDto ToDto(Car car);
}
