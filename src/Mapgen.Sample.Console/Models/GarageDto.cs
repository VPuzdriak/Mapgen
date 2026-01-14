using System.Collections.Immutable;

namespace Mapgen.Sample.Console.Models;

public record GarageDto(string Street, string City, int Number)
{
  public required IImmutableList<CarDto> Cars { get; init; }
}
