using Mapgen.Analyzer;
using Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.InheritanceConstructor.Models;

namespace Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.InheritanceConstructor;

/// <summary>
/// Mapper for derived class with base constructor
/// </summary>
[Mapper]
public partial class CarInheritanceMapper
{
  public partial CarDto ToDto(Car source);

  public CarInheritanceMapper()
  {
    UseConstructor(
      source => source.Make,      // Base class property
      source => source.Model,     // Base class property
      source => source.Year,      // Base class property
      source => source.NumberOfDoors  // Derived class property
    );
    // FuelType should be mapped via object initializer (required property)
  }
}
