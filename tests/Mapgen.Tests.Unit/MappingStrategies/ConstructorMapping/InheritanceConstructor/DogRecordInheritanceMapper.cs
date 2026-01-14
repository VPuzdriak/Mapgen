using Mapgen.Analyzer;
using Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.InheritanceConstructor.Models;

namespace Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.InheritanceConstructor;

/// <summary>
/// Mapper for derived record with base record constructor
/// </summary>
[Mapper]
public partial class DogRecordInheritanceMapper
{
  public partial DogDto ToDto(DogRecord source);

  public DogRecordInheritanceMapper()
  {
    UseConstructor(
      source => source.Species,       // Base record property
      source => source.Age,           // Base record property
      source => source.Breed,         // Derived record property
      source => source.IsVaccinated   // Derived record property
    );
  }
}
