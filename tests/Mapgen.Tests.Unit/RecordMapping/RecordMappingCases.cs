using FluentAssertions;

using Mapgen.Tests.Unit.RecordMapping.Models;

namespace Mapgen.Tests.Unit.RecordMapping;

public class RecordMappingCases
{
  [Fact]
  public void RecordToRecord_ShouldMapAllProperties_AndIgnoreEqualityContract()
  {
    // Arrange
    var source = new PersonRecord { Name = "John Doe", Age = 30 };
    var mapper = new PersonRecordMapper();

    // Act
    var result = mapper.ToDto(source);

    // Assert
    result.Name.Should().Be("John Doe");
    result.Age.Should().Be(30);
  }
}
