using FluentAssertions;

using Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.InheritanceConstructor.Models;

namespace Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.InheritanceConstructor;

/// <summary>
/// Tests for constructor mapping with inheritance (base class constructors)
/// </summary>
public class InheritanceConstructorCases
{
  #region Class Inheritance Tests

  [Fact]
  public void When_MappingDerivedClass_Should_MapBaseClassProperties()
  {
    // Arrange
    var car = new Car("Toyota", "Camry", 2023, 4)
    {
      FuelType = "Hybrid"
    };
    var mapper = new CarInheritanceMapper();

    // Act
    var result = mapper.ToDto(car);

    // Assert - Base class properties should be mapped
    result.Make.Should().Be(car.Make);
    result.Model.Should().Be(car.Model);
    result.Year.Should().Be(car.Year);
  }

  [Fact]
  public void When_MappingDerivedClass_Should_MapDerivedClassProperties()
  {
    // Arrange
    var car = new Car("Honda", "Accord", 2024, 2)
    {
      FuelType = "Electric"
    };
    var mapper = new CarInheritanceMapper();

    // Act
    var result = mapper.ToDto(car);

    // Assert - Derived class properties should be mapped
    result.NumberOfDoors.Should().Be(car.NumberOfDoors);
    result.FuelType.Should().Be(car.FuelType);
  }

  [Fact]
  public void When_MappingDerivedClass_Should_UseBaseConstructorCorrectly()
  {
    // Arrange
    var car = new Car("Ford", "Mustang", 2023, 2)
    {
      FuelType = "Gasoline"
    };
    var mapper = new CarInheritanceMapper();

    // Act
    var result = mapper.ToDto(car);

    // Assert - Constructor should set base and derived properties
    result.Should().NotBeNull();
    result.Make.Should().Be("Ford");
    result.Model.Should().Be("Mustang");
    result.Year.Should().Be(2023);
    result.NumberOfDoors.Should().Be(2);
  }

  [Fact]
  public void When_MappingDerivedClass_Should_MapRequiredPropertyViaInitializer()
  {
    // Arrange
    var car = new Car("Tesla", "Model 3", 2024, 4)
    {
      FuelType = "Electric"
    };
    var mapper = new CarInheritanceMapper();

    // Act
    var result = mapper.ToDto(car);

    // Assert - FuelType is required and should be mapped via initializer
    result.FuelType.Should().Be(car.FuelType);
  }

  [Fact]
  public void When_MappingDerivedClassWithInheritedReadonlyProperties_Should_PreserveImmutability()
  {
    // Arrange
    var car = new Car("BMW", "M3", 2023, 2)
    {
      FuelType = "Gasoline"
    };
    var mapper = new CarInheritanceMapper();

    // Act
    var result = mapper.ToDto(car);

    // Assert - Base class properties are readonly (set via constructor)
    result.Make.Should().Be("BMW");
    result.Model.Should().Be("M3");
    result.Year.Should().Be(2023);
    // Note: These properties are readonly and cannot be changed after construction
  }

  #endregion

  #region Record Inheritance Tests

  [Fact]
  public void When_MappingDerivedRecord_Should_MapBaseRecordProperties()
  {
    // Arrange
    var dog = new DogRecord("Dog", 5, "Golden Retriever", true);
    var mapper = new DogRecordInheritanceMapper();

    // Act
    var result = mapper.ToDto(dog);

    // Assert - Base record properties should be mapped
    result.Species.Should().Be(dog.Species);
    result.Age.Should().Be(dog.Age);
  }

  [Fact]
  public void When_MappingDerivedRecord_Should_MapDerivedRecordProperties()
  {
    // Arrange
    var dog = new DogRecord("Dog", 3, "Labrador", false);
    var mapper = new DogRecordInheritanceMapper();

    // Act
    var result = mapper.ToDto(dog);

    // Assert - Derived record properties should be mapped
    result.Breed.Should().Be(dog.Breed);
    result.IsVaccinated.Should().Be(dog.IsVaccinated);
  }

  [Fact]
  public void When_MappingDerivedRecord_Should_UseBaseRecordConstructorCorrectly()
  {
    // Arrange
    var dog = new DogRecord("Dog", 7, "German Shepherd", true);
    var mapper = new DogRecordInheritanceMapper();

    // Act
    var result = mapper.ToDto(dog);

    // Assert - Record constructor should set all properties via primary constructor
    result.Should().NotBeNull();
    result.Species.Should().Be("Dog");
    result.Age.Should().Be(7);
    result.Breed.Should().Be("German Shepherd");
    result.IsVaccinated.Should().Be(true);
  }

  [Fact]
  public void When_MappingDerivedRecord_Should_InheritRecordValueEquality()
  {
    // Arrange
    var dog1 = new DogRecord("Dog", 4, "Beagle", true);
    var dog2 = new DogRecord("Dog", 4, "Beagle", true);
    var mapper = new DogRecordInheritanceMapper();

    // Act
    var result1 = mapper.ToDto(dog1);
    var result2 = mapper.ToDto(dog2);

    // Assert - Records with same values should be equal (value-based equality)
    result1.Should().Be(result2);
    result1.GetHashCode().Should().Be(result2.GetHashCode());
  }

  [Fact]
  public void When_MappingDerivedRecord_Should_SupportWithExpression()
  {
    // Arrange
    var dog = new DogRecord("Dog", 2, "Poodle", false);
    var mapper = new DogRecordInheritanceMapper();

    // Act
    var result = mapper.ToDto(dog);
    var vaccinated = result with { IsVaccinated = true }; // Using 'with' expression

    // Assert - Original should be unchanged, new instance should be modified
    result.IsVaccinated.Should().BeFalse();
    vaccinated.IsVaccinated.Should().BeTrue();
    vaccinated.Breed.Should().Be(result.Breed);
    vaccinated.Species.Should().Be(result.Species);
  }

  #endregion

  #region Base and Derived Type Polymorphism Tests

  [Fact]
  public void When_MappingDerivedClass_Should_PreserveTypeHierarchy()
  {
    // Arrange
    var car = new Car("Audi", "A4", 2023, 4)
    {
      FuelType = "Diesel"
    };
    var mapper = new CarInheritanceMapper();

    // Act
    var result = mapper.ToDto(car);

    // Assert - Result should be of derived type
    result.Should().BeOfType<CarDto>();
    result.Should().BeAssignableTo<VehicleDto>(); // Should also be assignable to base type
  }

  [Fact]
  public void When_MappingDerivedRecord_Should_PreserveRecordTypeHierarchy()
  {
    // Arrange
    var dog = new DogRecord("Dog", 6, "Bulldog", true);
    var mapper = new DogRecordInheritanceMapper();

    // Act
    var result = mapper.ToDto(dog);

    // Assert - Result should be of derived record type
    result.Should().BeOfType<DogDto>();
    result.Should().BeAssignableTo<AnimalDto>(); // Should also be assignable to base record
  }

  #endregion

  #region Edge Cases with Inheritance

  [Fact]
  public void When_MappingDerivedClassWithEmptyStrings_Should_PreserveValues()
  {
    // Arrange
    var car = new Car(string.Empty, string.Empty, 0, 0)
    {
      FuelType = string.Empty
    };
    var mapper = new CarInheritanceMapper();

    // Act
    var result = mapper.ToDto(car);

    // Assert
    result.Make.Should().Be(string.Empty);
    result.Model.Should().Be(string.Empty);
    result.Year.Should().Be(0);
    result.NumberOfDoors.Should().Be(0);
    result.FuelType.Should().Be(string.Empty);
  }

  [Fact]
  public void When_MappingDerivedRecordWithEmptyStrings_Should_PreserveValues()
  {
    // Arrange
    var dog = new DogRecord(string.Empty, 0, string.Empty, false);
    var mapper = new DogRecordInheritanceMapper();

    // Act
    var result = mapper.ToDto(dog);

    // Assert
    result.Species.Should().Be(string.Empty);
    result.Age.Should().Be(0);
    result.Breed.Should().Be(string.Empty);
    result.IsVaccinated.Should().BeFalse();
  }

  [Fact]
  public void When_MappingDerivedClassWithSpecialCharacters_Should_PreserveValues()
  {
    // Arrange
    var car = new Car("Mercedes-Benz", "C-Class", 2023, 4)
    {
      FuelType = "Diesel/Electric"
    };
    var mapper = new CarInheritanceMapper();

    // Act
    var result = mapper.ToDto(car);

    // Assert
    result.Make.Should().Be("Mercedes-Benz");
    result.Model.Should().Be("C-Class");
    result.FuelType.Should().Be("Diesel/Electric");
  }

  [Fact]
  public void When_MappingDerivedRecordWithSpecialCharacters_Should_PreserveValues()
  {
    // Arrange
    var dog = new DogRecord("Dog/Canine", 5, "St. Bernard", true);
    var mapper = new DogRecordInheritanceMapper();

    // Act
    var result = mapper.ToDto(dog);

    // Assert
    result.Species.Should().Be("Dog/Canine");
    result.Breed.Should().Be("St. Bernard");
  }

  [Fact]
  public void When_MappingDerivedClassWithNegativeValues_Should_PreserveValues()
  {
    // Arrange (edge case - negative year)
    var car = new Car("Unknown", "Unknown", -1, -1)
    {
      FuelType = "Unknown"
    };
    var mapper = new CarInheritanceMapper();

    // Act
    var result = mapper.ToDto(car);

    // Assert
    result.Year.Should().Be(-1);
    result.NumberOfDoors.Should().Be(-1);
  }

  [Fact]
  public void When_MappingDerivedRecordWithNegativeValues_Should_PreserveValues()
  {
    // Arrange (edge case - negative age)
    var dog = new DogRecord("Unknown", -1, "Unknown", false);
    var mapper = new DogRecordInheritanceMapper();

    // Act
    var result = mapper.ToDto(dog);

    // Assert
    result.Age.Should().Be(-1);
  }

  #endregion

  #region Multiple Instance Tests

  [Fact]
  public void When_MappingMultipleDerivedClasses_Should_CreateSeparateInstances()
  {
    // Arrange
    var car1 = new Car("Toyota", "Corolla", 2023, 4) { FuelType = "Gasoline" };
    var car2 = new Car("Honda", "Civic", 2024, 4) { FuelType = "Hybrid" };
    var mapper = new CarInheritanceMapper();

    // Act
    var result1 = mapper.ToDto(car1);
    var result2 = mapper.ToDto(car2);

    // Assert
    result1.Should().NotBeSameAs(result2);
    result1.Make.Should().Be("Toyota");
    result2.Make.Should().Be("Honda");
  }

  [Fact]
  public void When_MappingMultipleDerivedRecords_Should_CreateSeparateInstances()
  {
    // Arrange
    var dog1 = new DogRecord("Dog", 3, "Chihuahua", true);
    var dog2 = new DogRecord("Dog", 5, "Husky", false);
    var mapper = new DogRecordInheritanceMapper();

    // Act
    var result1 = mapper.ToDto(dog1);
    var result2 = mapper.ToDto(dog2);

    // Assert
    result1.Should().NotBeSameAs(result2);
    result1.Should().NotBe(result2); // Different values
    result1.Breed.Should().Be("Chihuahua");
    result2.Breed.Should().Be("Husky");
  }

  [Fact]
  public void When_MappingSameDerivedClassMultipleTimes_Should_CreateNewInstancesEachTime()
  {
    // Arrange
    var car = new Car("Nissan", "Altima", 2023, 4) { FuelType = "Gasoline" };
    var mapper = new CarInheritanceMapper();

    // Act
    var result1 = mapper.ToDto(car);
    var result2 = mapper.ToDto(car);

    // Assert - Should be different instances
    result1.Should().NotBeSameAs(result2);
  }

  [Fact]
  public void When_MappingSameDerivedRecordMultipleTimes_Should_CreateNewInstancesEachTime()
  {
    // Arrange
    var dog = new DogRecord("Dog", 4, "Dalmatian", true);
    var mapper = new DogRecordInheritanceMapper();

    // Act
    var result1 = mapper.ToDto(dog);
    var result2 = mapper.ToDto(dog);

    // Assert - Should be different instances but equal values (record equality)
    result1.Should().NotBeSameAs(result2);
    result1.Should().Be(result2); // Value equality
  }

  #endregion

  #region Constructor Chain Verification Tests

  [Fact]
  public void When_MappingDerivedClass_Should_ConstructorChainExecuteCorrectly()
  {
    // Arrange
    var car = new Car("Chevrolet", "Malibu", 2023, 4)
    {
      FuelType = "Gasoline"
    };
    var mapper = new CarInheritanceMapper();

    // Act
    var result = mapper.ToDto(car);

    // Assert - All properties should be set, indicating constructor chain worked
    result.Make.Should().NotBeNull();
    result.Model.Should().NotBeNull();
    result.Year.Should().BeGreaterThan(0);
    result.NumberOfDoors.Should().BeGreaterThan(0);
    result.FuelType.Should().NotBeNull();
  }

  [Fact]
  public void When_MappingDerivedRecord_Should_PrimaryConstructorChainExecuteCorrectly()
  {
    // Arrange
    var dog = new DogRecord("Dog", 8, "Rottweiler", true);
    var mapper = new DogRecordInheritanceMapper();

    // Act
    var result = mapper.ToDto(dog);

    // Assert - All properties should be set via primary constructor chain
    result.Species.Should().NotBeNull();
    result.Age.Should().BeGreaterThan(0);
    result.Breed.Should().NotBeNull();
    result.IsVaccinated.Should().BeTrue();
  }

  #endregion
}
