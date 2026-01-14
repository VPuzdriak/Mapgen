namespace Mapgen.Sample.Console.Models;

public class Garage
{
  public required string Address { get; init; }
  public required List<Car> Cars { get; init; }
}
