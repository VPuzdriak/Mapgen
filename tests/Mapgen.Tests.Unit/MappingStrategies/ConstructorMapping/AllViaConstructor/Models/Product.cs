namespace Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.AllViaConstructor.Models;

/// <summary>
/// Source model
/// </summary>
public class Product
{
  public required string Name { get; init; }
  public required decimal Price { get; init; }
}
