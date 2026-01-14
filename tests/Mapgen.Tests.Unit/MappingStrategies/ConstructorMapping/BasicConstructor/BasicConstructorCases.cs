using FluentAssertions;

using Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.BasicConstructor.Models;

namespace Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.BasicConstructor;

/// <summary>
/// Tests for basic constructor mapping with 3 parameters + object initializer
/// </summary>
public class BasicConstructorCases
{
  [Fact]
  public void When_UsingConstructorWithThreeParameters_Should_MapConstructorArguments()
  {
    // Arrange
    var product = new Product
    {
      Name = "Laptop",
      Description = "High-performance laptop",
      Price = 1299.99m,
      Stock = 50,
      Category = "Electronics"
    };
    var mapper = new BasicConstructorProductMapper();

    // Act
    var result = mapper.ToDto(product);

    // Assert
    result.Name.Should().Be(product.Name);
    result.Description.Should().Be(product.Description);
    result.Price.Should().Be(product.Price);
  }

  [Fact]
  public void When_UsingConstructorWithThreeParameters_Should_MapRemainingPropertiesViaInitializer()
  {
    // Arrange
    var product = new Product
    {
      Name = "Laptop",
      Description = "High-performance laptop",
      Price = 1299.99m,
      Stock = 50,
      Category = "Electronics"
    };
    var mapper = new BasicConstructorProductMapper();

    // Act
    var result = mapper.ToDto(product);

    // Assert - Stock and Category should be mapped via object initializer
    result.Stock.Should().Be(product.Stock);
    result.Category.Should().Be(product.Category);
  }

  [Fact]
  public void When_MappingReadonlyPropertiesViaConstructor_Should_SetValuesCorrectly()
  {
    // Arrange
    var product = new Product
    {
      Name = "Tablet",
      Description = "10-inch tablet",
      Price = 399.99m,
      Stock = 25,
      Category = "Electronics"
    };
    var mapper = new BasicConstructorProductMapper();

    // Act
    var result = mapper.ToDto(product);

    // Assert - Name, Description, Price are readonly and set via constructor
    result.Name.Should().Be(product.Name);
    result.Description.Should().Be(product.Description);
    result.Price.Should().Be(product.Price);
  }

  [Fact]
  public void When_MappingRequiredProperties_Should_MapViaObjectInitializer()
  {
    // Arrange
    var product = new Product
    {
      Name = "Keyboard",
      Description = "Mechanical keyboard",
      Price = 149.99m,
      Stock = 75,
      Category = "Accessories"
    };
    var mapper = new BasicConstructorProductMapper();

    // Act
    var result = mapper.ToDto(product);

    // Assert - Category is required and should be mapped via initializer
    result.Category.Should().Be(product.Category);
  }

  [Fact]
  public void When_MappingWithEmptyStrings_Should_PreserveEmptyValues()
  {
    // Arrange
    var product = new Product
    {
      Name = string.Empty,
      Description = string.Empty,
      Price = 0m,
      Stock = 0,
      Category = string.Empty
    };
    var mapper = new BasicConstructorProductMapper();

    // Act
    var result = mapper.ToDto(product);

    // Assert
    result.Name.Should().Be(string.Empty);
    result.Description.Should().Be(string.Empty);
    result.Price.Should().Be(0m);
    result.Stock.Should().Be(0);
    result.Category.Should().Be(string.Empty);
  }

  [Fact]
  public void When_MappingWithSpecialCharacters_Should_PreserveValues()
  {
    // Arrange
    var product = new Product
    {
      Name = "Product <>&\"'",
      Description = "Description with special chars: !@#$%^&*()",
      Price = 99.99m,
      Stock = 10,
      Category = "Category/Subcategory"
    };
    var mapper = new BasicConstructorProductMapper();

    // Act
    var result = mapper.ToDto(product);

    // Assert
    result.Name.Should().Be(product.Name);
    result.Description.Should().Be(product.Description);
    result.Category.Should().Be(product.Category);
  }

  [Fact]
  public void When_MappingMultipleInstances_Should_CreateSeparateObjects()
  {
    // Arrange
    var product1 = new Product
    {
      Name = "Product1",
      Description = "Description1",
      Price = 100m,
      Stock = 10,
      Category = "Cat1"
    };

    var product2 = new Product
    {
      Name = "Product2",
      Description = "Description2",
      Price = 200m,
      Stock = 20,
      Category = "Cat2"
    };
    var mapper = new BasicConstructorProductMapper();

    // Act
    var result1 = mapper.ToDto(product1);
    var result2 = mapper.ToDto(product2);

    // Assert - Should be separate instances with different values
    result1.Should().NotBeSameAs(result2);
    result1.Name.Should().Be("Product1");
    result2.Name.Should().Be("Product2");
    result1.Price.Should().Be(100m);
    result2.Price.Should().Be(200m);
  }
}
