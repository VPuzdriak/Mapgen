namespace Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.MultipleConstructors.Models;

/// <summary>
/// Source model for order mapping
/// </summary>
public class Order
{
  public required int OrderId { get; init; }
  public required string Status { get; init; }
  public required decimal Total { get; init; }
}
