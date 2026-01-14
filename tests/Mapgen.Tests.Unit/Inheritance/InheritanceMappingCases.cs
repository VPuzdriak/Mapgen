using FluentAssertions;

using Mapgen.Tests.Unit.Inheritance.Models;

namespace Mapgen.Tests.Unit.Inheritance;

public class InheritanceMappingCases
{
  [Fact]
  public void When_MappingDerivedClass_Then_BaseClassPropertiesAreMapped()
  {
    // Arrange
    var mapper = new CarMapper();
    var car = new Car
    {
      Id = Guid.NewGuid(),
      Make = "Toyota",
      Model = "Camry",
      NumberOfDoors = 4,
    };

    // Act
    var carDto = mapper.ToDto(car);

    carDto.Id.Should().Be(car.Id);
    carDto.Make.Should().Be(car.Make);
    carDto.Model.Should().Be(car.Model);
    carDto.NumberOfDoors.Should().Be(car.NumberOfDoors);
  }
}
