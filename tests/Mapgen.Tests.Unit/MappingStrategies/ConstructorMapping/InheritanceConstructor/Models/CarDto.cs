namespace Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.InheritanceConstructor.Models;

/// <summary>
/// Derived DTO class that calls base constructor
/// </summary>
public class CarDto : VehicleDto
{
  public int NumberOfDoors { get; }
  public required string FuelType { get; init; }

  public CarDto(string make, string model, int year, int numberOfDoors)
    : base(make, model, year)
  {
    NumberOfDoors = numberOfDoors;
  }
}
