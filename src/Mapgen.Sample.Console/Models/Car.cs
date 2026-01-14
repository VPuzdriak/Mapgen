namespace Mapgen.Sample.Console.Models;

public class Car : Vehicle
{
  public required CarOwner Owner { get; init; }
}
