using FluentAssertions;

using Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.AllViaConstructor.Models;

namespace Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.AllViaConstructor;

/// <summary>
/// Tests for mapping where all properties are set via constructor
/// </summary>
public class AllViaConstructorCases
{
  [Fact]
  public void When_AllPropertiesAreSetViaConstructor_Should_MapSuccessfully()
  {
    // Arrange
    var product = new Product
    {
      Name = "Mouse",
      Price = 29.99m
    };

    // Act
    var result = new AllViaConstructorProductMapper().ToDto(product);

    // Assert
    result.Name.Should().Be(product.Name);
    result.Price.Should().Be(product.Price);
  }

  [Fact]
  public void When_MappingDecimalValues_Should_PreservePrecision()
  {
    // Arrange
    var product = new Product
    {
      Name = "Precision Item",
      Price = 123.456789m
    };
    var mapper = new AllViaConstructorProductMapper();

    // Act
    var result = mapper.ToDto(product);

    // Assert
    result.Price.Should().Be(123.456789m);
  }

  [Fact]
  public void When_MappingNegativeValues_Should_PreserveSign()
  {
    // Arrange
    var product = new Product
    {
      Name = "Refund",
      Price = -50.00m
    };
    var mapper = new AllViaConstructorProductMapper();

    // Act
    var result = mapper.ToDto(product);

    // Assert
    result.Price.Should().Be(-50.00m);
  }

  [Fact]
  public void When_MappingSameSourceMultipleTimes_Should_CreateNewInstancesEachTime()
  {
    // Arrange
    var product = new Product
    {
      Name = "Product",
      Price = 100m
    };
    var mapper = new AllViaConstructorProductMapper();

    // Act
    var result1 = mapper.ToDto(product);
    var result2 = mapper.ToDto(product);

    // Assert - Should be different instances with same values
    result1.Should().NotBeSameAs(result2);
    result1.Name.Should().Be(result2.Name);
    result1.Price.Should().Be(result2.Price);
  }
}
