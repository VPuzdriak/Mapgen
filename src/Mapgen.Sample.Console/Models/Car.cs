namespace Mapgen.Sample.Console.Models;

public class Car
{
  public required string Make { get; init; }
  public required string Model { get; init; }
  public required int ReleaseYear { get; init; }
  public required CarOwner Owner { get; init; }
}
