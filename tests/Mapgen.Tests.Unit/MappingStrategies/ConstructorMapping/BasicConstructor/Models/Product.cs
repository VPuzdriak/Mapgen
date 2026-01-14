namespace Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.BasicConstructor.Models;

/// <summary>
/// Source model for basic constructor mapping
/// </summary>
public class Product
{
  public required string Name { get; init; }
  public required string Description { get; init; }
  public required decimal Price { get; init; }
  public required int Stock { get; init; }
  public required string Category { get; init; }
}
