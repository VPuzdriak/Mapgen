using FluentAssertions;

using Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.AutoConstructor.Models;

namespace Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.AutoConstructor;

/// <summary>
/// Tests for automatic constructor detection and mapping
/// </summary>
public class AutoConstructorCases
{
  [Fact]
  public void When_ConstructorParametersMatchSourceProperties_Should_AutoMapConstructor()
  {
    // Arrange
    var product = new Product
    {
      Name = "Laptop",
      Price = 999.99m,
      Category = "Electronics"
    };
    var mapper = new AutoProductMapper();

    // Act
    var result = mapper.ToDto(product);

    // Assert - Constructor parameters should be auto-mapped
    result.Name.Should().Be(product.Name);
    result.Price.Should().Be(product.Price);
    result.Category.Should().Be(product.Category);
  }

  [Fact]
  public void When_AutoMappingConstructor_Should_MapRemainingPropertiesViaInitializer()
  {
    // Arrange
    var product = new Product
    {
      Name = "Mouse",
      Price = 29.99m,
      Category = "Accessories"
    };
    var mapper = new AutoProductMapper();

    // Act
    var result = mapper.ToDto(product);

    // Assert - Category should be mapped via object initializer
    result.Category.Should().Be("Accessories");
  }

  [Fact]
  public void When_AutoMappingConstructor_Should_CreateValidInstance()
  {
    // Arrange
    var product = new Product
    {
      Name = "Keyboard",
      Price = 79.99m,
      Category = "Peripherals"
    };
    var mapper = new AutoProductMapper();

    // Act
    var result = mapper.ToDto(product);

    // Assert
    result.Should().NotBeNull();
    result.Name.Should().NotBeEmpty();
    result.Price.Should().BeGreaterThan(0);
  }

  [Fact]
  public void When_ConstructorParametersHaveImplicitConversion_Should_AutoMapConstructor()
  {
    // Arrange - Person has int Age, PersonDto has long Age (implicit conversion)
    var person = new Person
    {
      Name = "John Doe",
      Age = 30
    };
    var mapper = new AutoPersonMapper();

    // Act
    var result = mapper.ToDto(person);

    // Assert - Should auto-map even with implicit conversion
    result.Name.Should().Be(person.Name);
    result.Age.Should().Be(person.Age);
  }

  [Fact]
  public void When_AutoMappingWithImplicitConversion_Should_PreserveValues()
  {
    // Arrange
    var person = new Person
    {
      Name = "Jane Smith",
      Age = 25
    };
    var mapper = new AutoPersonMapper();

    // Act
    var result = mapper.ToDto(person);

    // Assert - Implicit int to long conversion should work correctly
    result.Age.Should().Be(25L);
  }
}
