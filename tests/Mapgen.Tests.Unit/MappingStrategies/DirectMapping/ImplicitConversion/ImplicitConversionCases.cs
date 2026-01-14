using FluentAssertions;

using Mapgen.Tests.Unit.MappingStrategies.DirectMapping.ImplicitConversion.Models;

namespace Mapgen.Tests.Unit.MappingStrategies.DirectMapping.ImplicitConversion;

public class ImplicitConversionCases
{
  [Fact]
  public void When_NamesMatch_And_FloatToDecimal_MapSuccessfully()
  {
    // Arrange
    var source = new Product { Id = 123, Price = 99.99F };

    // Act
    var result = source.ToDto();

    // Assert
    result.Id.Should().Be(source.Id);
    result.Price.Should().Be(source.Price);
  }

  [Fact]
  public void When_NamesMatch_And_DestinationIsNullable_MapSuccessfully()
  {
    // Arrange
    var source = new Contact
    {
      Id = Guid.NewGuid(),
      Email = "test@example.com",
      Phone = "123-456-7890"
    };

    // Act
    var result = source.ToDto();

    // Assert
    result.Id.Should().Be(source.Id);
    result.Email.Should().Be(source.Email);
    result.Phone.Should().Be(source.Phone);
  }
}
