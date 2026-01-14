using FluentAssertions;

using Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.MultipleConstructors.Models;

namespace Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.MultipleConstructors;

/// <summary>
/// Tests for constructor selection when destination has multiple constructors
/// </summary>
public class MultipleConstructorsCases
{
  [Fact]
  public void When_SelectingConstructorWithOneParameter_Should_UseCorrectConstructor()
  {
    // Arrange
    var order = new Order
    {
      OrderId = 12345,
      Status = "Shipped",
      Total = 599.99m
    };

    // Act
    var result = new OrderMapperSingleParam().ToDto(order);

    // Assert
    result.OrderId.Should().Be(order.OrderId);
    result.Status.Should().Be("Pending"); // Default value from constructor
    result.Total.Should().Be(0); // Default value from constructor
  }

  [Fact]
  public void When_SelectingConstructorWithAllParameters_Should_UseFullConstructor()
  {
    // Arrange
    var order = new Order
    {
      OrderId = 12345,
      Status = "Shipped",
      Total = 599.99m
    };

    // Act
    var result = new OrderMapperAllParams().ToDto(order);

    // Assert
    result.OrderId.Should().Be(order.OrderId);
    result.Status.Should().Be(order.Status);
    result.Total.Should().Be(order.Total);
  }
}
