namespace Mapgen.Sample.Console.Models;

public class CarDto : VehicleDto
{
  public required string CountryOfOrigin { get; init; }
  public required CarOwnerDto Owner { get; init; }
  public required DriverDto MainDriver { get; init; }
}

public abstract class VehicleDto
{
  public required string Make { get; init; }
  public required string Model { get; init; }
  public required int ReleaseYear { get; init; }
}
