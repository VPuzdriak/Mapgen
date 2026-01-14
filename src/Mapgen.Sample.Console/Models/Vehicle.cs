namespace Mapgen.Sample.Console.Models;

public abstract class Vehicle
{
  public required string Make { get; init; }
  public required string Model { get; init; }
  public required int ReleaseYear { get; init; }
}
