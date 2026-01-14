namespace Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.InheritanceConstructor.Models;

/// <summary>
/// Derived DTO record that extends base record
/// </summary>
public record DogDto(string Species, int Age, string Breed, bool IsVaccinated)
  : AnimalDto(Species, Age);
