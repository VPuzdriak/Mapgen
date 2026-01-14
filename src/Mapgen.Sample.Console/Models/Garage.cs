namespace Mapgen.Sample.Console.Models;

public class Garage
{
  public required string Street { get; init; }
  public required string City { get; init; }
  public required int Number { get; init; }
  public required List<Car> Cars { get; init; }
}
