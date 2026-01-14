using System.Collections.Immutable;

namespace Mapgen.Sample.Console.Models;

public class GarageDto
{
  public required string Address { get; init; }
  public required IImmutableList<CarDto> Cars { get; init; }
}
