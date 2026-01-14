namespace Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.InheritanceConstructor.Models;

/// <summary>
/// Derived class that calls base constructor
/// </summary>
public class Car : Vehicle
{
  public int NumberOfDoors { get; }
  public string FuelType { get; set; }

  public Car(string make, string model, int year, int numberOfDoors)
    : base(make, model, year)
  {
    NumberOfDoors = numberOfDoors;
    FuelType = "Gasoline";
  }
}
