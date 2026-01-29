using System;

using FluentAssertions;

using Mapgen.Tests.Unit.MappingStrategies.IgnoreMapping.Models;

namespace Mapgen.Tests.Unit.MappingStrategies.IgnoreMapping;

public class IgnoreMappingCases
{
  [Fact]
  public void When_MemberIsIgnored_MemberRemainsWithDefault()
  {
    // Arrange
    var source = new User { Id = Guid.NewGuid(), Username = "johndoe", Password = "secret123" };

    // Act
    var result = source.ToDto();

    // Assert
    result.Id.Should().Be(source.Id);
    result.Username.Should().Be(source.Username);
    result.Nationality.Should().BeNull();
  }
}
