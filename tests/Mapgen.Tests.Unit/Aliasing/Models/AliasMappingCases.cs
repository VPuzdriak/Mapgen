using FluentAssertions;

namespace Mapgen.Tests.Unit.Aliasing.Models;

public class AliasMappingCases
{
  [Fact]
  public void When_TypesWithAliasedNames_ShouldMapCorrectly()
  {
    // Arrange
    var person = new Entity.Person { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe" };
    var partner = new Entity.Person { Id = Guid.NewGuid(), FirstName = "Jane", LastName = "Smith" };
    var mapper = new PersonAliasMapper();

    // Act
    var result = mapper.ToContract(person, partner);

    // Assert
    result.Id.Should().Be(person.Id);
    result.FullName.Should().Be("John Doe");
  }
}
