using System;

using FluentAssertions;

using Mapgen.Tests.Unit.FieldsMapping.Models.Entities;

namespace Mapgen.Tests.Unit.FieldsMapping;

public class FieldsMappingCases
{
  [Fact]
  public void When_PublicFields_ShouldMapCorrectly()
  {
    // Arrange
    var source = new Person { Id = Guid.NewGuid(), FirstName = "Alice", LastName = "Johnson" };
    var mapper = new PublicFieldsMapper();

    // Act
    var result = mapper.ToContract(source);

    // Assert
    result.Id.Should().Be(source.Id);
    result.FullName.Should().Be("Alice Johnson");
  }
}
