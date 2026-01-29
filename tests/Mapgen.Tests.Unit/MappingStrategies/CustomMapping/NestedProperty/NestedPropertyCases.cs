using System;

using FluentAssertions;

using Mapgen.Tests.Unit.MappingStrategies.CustomMapping.NestedProperty.Models;

namespace Mapgen.Tests.Unit.MappingStrategies.CustomMapping.NestedProperty;

public class NestedPropertyCases
{
  [Fact]
  public void When_NestedProperty_MapSuccessfully()
  {
    // Arrange
    var source = new Employee { Id = Guid.NewGuid(), Name = "Jane Smith", Department = new Department { Name = "Engineering", Location = "Amsterdam" } };

    // Act
    var result = source.ToDto();

    // Assert top level properties
    result.Id.Should().Be(source.Id);
    result.Name.Should().Be(source.Name);
    // Assert nested properties
    result.Department.Name.Should().Be(source.Department.Name);
    result.Department.Location.Should().Be(source.Department.Location);
  }
}
