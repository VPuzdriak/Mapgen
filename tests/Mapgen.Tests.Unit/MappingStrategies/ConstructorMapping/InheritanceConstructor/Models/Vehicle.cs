namespace Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.InheritanceConstructor.Models;

/// <summary>
/// Base class with constructor
/// </summary>
public class Vehicle
{
  public string Make { get; }
  public string Model { get; }
  public int Year { get; }

  public Vehicle(string make, string model, int year)
  {
    Make = make;
    Model = model;
    Year = year;
  }
}
