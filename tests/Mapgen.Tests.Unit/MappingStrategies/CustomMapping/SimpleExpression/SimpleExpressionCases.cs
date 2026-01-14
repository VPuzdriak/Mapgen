using FluentAssertions;

using Mapgen.Tests.Unit.MappingStrategies.CustomMapping.SimpleExpression.Models;

namespace Mapgen.Tests.Unit.MappingStrategies.CustomMapping.SimpleExpression;

public class SimpleExpressionCases
{
  [Fact]
  public void When_SimpleExpression_MapSuccessfully()
  {
    // Arrange
    var source = new Customer { CustomerId = Guid.NewGuid(), FirstName = "John", LastName = "Doe" };

    // Act
    var result = source.ToDto();

    // Assert
    string expectedFullName = $"{source.FirstName} {source.LastName}";

    result.Id.Should().Be(source.CustomerId);
    result.FullName.Should().Be(expectedFullName);
  }
}
