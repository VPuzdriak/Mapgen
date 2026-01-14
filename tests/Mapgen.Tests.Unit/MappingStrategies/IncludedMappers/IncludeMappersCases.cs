using FluentAssertions;

using Mapgen.Tests.Unit.MappingStrategies.IncludedMappers.Models;

namespace Mapgen.Tests.Unit.MappingStrategies.IncludedMappers;

public class IncludeMappersCases
{
  [Fact]
  public void When_MapperIsIncluded_MapSuccessfully()
  {
    // Arrange
    var studio = new GamingStudio
    {
      Id = Guid.NewGuid(),
      Name = "Epic Games",
      FoundedYear = 1991,
      Publisher = new Publisher { Id = Guid.NewGuid(), Name = "Epic Games Publishing", Country = "USA", Revenue = 5400000000m }
    };

    // Act
    var mapper = new GamingStudioMapper();
    var result = mapper.ToDto(studio);

    // Assert
    result.Id.Should().Be(studio.Id);
    result.Name.Should().Be(studio.Name);
    result.FoundedYear.Should().Be(studio.FoundedYear);
    result.Publisher.Should().NotBeNull();
    result.Publisher.Id.Should().Be(studio.Publisher.Id);
    result.Publisher.Name.Should().Be(studio.Publisher.Name);
    result.Publisher.Country.Should().Be(studio.Publisher.Country);
    result.Publisher.Revenue.Should().Be(studio.Publisher.Revenue);
  }
}
