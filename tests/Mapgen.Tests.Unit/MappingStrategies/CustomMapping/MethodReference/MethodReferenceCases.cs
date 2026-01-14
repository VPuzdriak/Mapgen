using FluentAssertions;

using Mapgen.Tests.Unit.MappingStrategies.CustomMapping.MethodReference.Models;

namespace Mapgen.Tests.Unit.MappingStrategies.CustomMapping.MethodReference;

public class MethodReferenceCases
{
  [Theory]
  [InlineData(1000.00, "€1000")]
  [InlineData(1000.12, "€1000,12")]
  public void When_MethodReference_MapSuccessfully(decimal amount, string expectedFormattedAmount)
  {
    // Arrange
    var source = new Invoice { InvoiceNumber = "INV-2026-001", Amount = amount };

    // Act
    var result = source.ToDto();

    // Assert
    result.Number.Should().Be(source.InvoiceNumber);
    result.FormattedAmount.Should().Be(expectedFormattedAmount);
  }
}
