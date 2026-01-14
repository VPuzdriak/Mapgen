namespace Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.AutoConstructor.Models;

/// <summary>
/// DTO with single constructor that matches source properties exactly
/// </summary>
public class ProductDto
{
  public string Name { get; }
  public decimal Price { get; }
  public required string Category { get; init; }

  public ProductDto(string name, decimal price)
  {
    Name = name;
    Price = price;
  }
}
