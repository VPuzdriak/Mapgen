using System;

using FluentAssertions;

namespace Mapgen.Tests.Unit.FullNameTypes;

public class FullnameTypesCases
{
  [Fact]
  public void When_TypesWithFullNames_ShouldMapCorrectly()
  {
    // Arrange
    var person = new Models.Entity.Person { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe" };
    var partner = new Models.Entity.Person { Id = Guid.NewGuid(), FirstName = "Jane", LastName = "Smith" };
    var mapper = new PersonFullNameMapper();

    // Act
    var result = mapper.ToContract(person, partner);

    // Assert
    result.Id.Should().Be(person.Id);
    result.FullName.Should().Be("John Doe");
  }
}
