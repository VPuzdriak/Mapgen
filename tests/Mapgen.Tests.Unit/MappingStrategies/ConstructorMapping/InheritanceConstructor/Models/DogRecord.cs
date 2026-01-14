namespace Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.InheritanceConstructor.Models;

/// <summary>
/// Derived record that extends base record
/// </summary>
public record DogRecord(string Species, int Age, string Breed, bool IsVaccinated)
  : AnimalRecord(Species, Age);
