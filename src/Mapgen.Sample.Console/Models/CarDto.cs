namespace Mapgen.Sample.Console.Models;

public class CarDto
{
  public required string Make { get; init; }
  public required string Model { get; init; }
  public required int ReleaseYear { get; init; }
  public required string CountryOfOrigin { get; init; }
  public required CarOwnerDto Owner { get; init; }
  public required DriverDto MainDriver { get; init; }
}
