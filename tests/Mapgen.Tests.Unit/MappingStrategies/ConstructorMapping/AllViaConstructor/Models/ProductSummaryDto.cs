namespace Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.AllViaConstructor.Models;

/// <summary>
/// DTO with all properties set via constructor
/// </summary>
public class ProductSummaryDto
{
  public string Name { get; }
  public decimal Price { get; }

  public ProductSummaryDto(string name, decimal price)
  {
    Name = name;
    Price = price;
  }
}
