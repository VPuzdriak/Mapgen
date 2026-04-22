using FluentAssertions;

namespace Mapgen.Tests.Unit.FullNameQualifiers.Models;

public class FullNameQualifiersCases
{
  [Fact]
  public void When_UseFullNameQualifiersIsTrue_ShouldGenerateCodeWithFullyQualifiedNames()
  {
    // Arrange
    var product = new Entity.Product { Id = 1, ProductName = "Test Product", ProductPrice = 99.99m };
    var mapper = new ProductWithFullNamesMapper();

    // Act
    var result = mapper.ToContract(product);

    // Assert
    result.Id.Should().Be(1);
    result.Name.Should().Be("Test Product");
    result.Price.Should().Be(99.99m);
  }
}

