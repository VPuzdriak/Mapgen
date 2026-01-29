using System;

using FluentAssertions;

using Mapgen.Tests.Unit.MappingStrategies.DirectMapping.ExactMatch.Models;

namespace Mapgen.Tests.Unit.MappingStrategies.DirectMapping.ExactMatch;

public class ExactMatchCases
{
  [Fact]
  public void When_NamesAndTypesMatch_Should_MapSuccessfully()
  {
    // Arrange
    var source = new Person { Id = Guid.NewGuid(), Name = "John Doe", Age = 30 };

    // Act
    var result = source.ToDto();

    // Assert
    result.Id.Should().Be(source.Id);
    result.Name.Should().Be(source.Name);
    result.Age.Should().Be(source.Age);
  }
}

