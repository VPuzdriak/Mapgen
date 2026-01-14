using FluentAssertions;

using Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.EmptyConstructor.Models;

namespace Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.EmptyConstructor;

/// <summary>
/// Tests for UseEmptyConstructor functionality
/// </summary>
public class EmptyConstructorCases
{
  [Fact]
  public void When_UsingEmptyConstructor_Should_MapViaObjectInitializer()
  {
    // Arrange
    var customer = new Customer
    {
      Name = "John Doe",
      Email = "john@example.com"
    };

    // Act
    var result = new CustomerMapperEmptyConstructor().ToDto(customer);

    // Assert
    result.Name.Should().Be(customer.Name);
    result.Email.Should().Be(customer.Email);
  }

  [Fact]
  public void When_ExplicitlySelectingParameterizedConstructor_Should_UseConstructor()
  {
    // Arrange
    var customer = new Customer
    {
      Name = "Jane Smith",
      Email = "jane@example.com"
    };

    // Act
    var result = new CustomerMapperWithConstructor().ToDto(customer);

    // Assert
    result.Name.Should().Be(customer.Name);
    result.Email.Should().Be(customer.Email);
  }
}
