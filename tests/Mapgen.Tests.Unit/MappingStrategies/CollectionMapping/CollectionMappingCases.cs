using FluentAssertions;

using Mapgen.Tests.Unit.MappingStrategies.CollectionMapping.Models;

namespace Mapgen.Tests.Unit.MappingStrategies.CollectionMapping;

public class CollectionMappingCases
{
  [Fact]
  public void When_MapCollection_CollectionIsMappedSuccessfully()
  {
    // Arrange
    var source = new Team
    {
      Id = Guid.NewGuid(),
      Name = "Development Team",
      Members =
      [
        new() { Id = Guid.NewGuid(), Name = "Alice" },
        new() { Id = Guid.NewGuid(), Name = "Bob" }
      ]
    };

    var lead = new Lead { Guid = Guid.NewGuid(), Name = "Team Lead" };

    // Act
    var result = source.ToDto(lead);

    // Assert
    result.Id.Should().Be(source.Id);
    result.Name.Should().Be(source.Name);
    result.Members.Should().HaveCount(1);
    result.Members[0].Name.Should().Be("Alice");
  }

  [Fact]
  public void When_MapCollection_WithExtraArguments_CollectionIsMappedSuccessfully()
  {
  }
}
