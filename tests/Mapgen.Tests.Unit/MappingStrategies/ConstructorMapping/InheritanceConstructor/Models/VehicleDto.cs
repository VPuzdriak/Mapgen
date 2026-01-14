namespace Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.InheritanceConstructor.Models;

/// <summary>
/// Base DTO class with constructor
/// </summary>
public class VehicleDto
{
  public string Make { get; }
  public string Model { get; }
  public int Year { get; }

  public VehicleDto(string make, string model, int year)
  {
    Make = make;
    Model = model;
    Year = year;
  }
}
