namespace Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.BasicConstructor.Models;

/// <summary>
/// DTO with readonly properties set via constructor and settable/required properties via initializer
/// </summary>
public class ProductDto
{
  public string Name { get; }
  public string Description { get; }
  public decimal Price { get; }
  public int Stock { get; set; } // Settable property - via object initializer
  public required string Category { get; init; } // Required property - via object initializer

  public ProductDto(string name, string description, decimal price)
  {
    Name = name;
    Description = description;
    Price = price;
  }
}
