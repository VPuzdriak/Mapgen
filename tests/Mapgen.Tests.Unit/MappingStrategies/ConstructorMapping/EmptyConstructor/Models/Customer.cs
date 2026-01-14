namespace Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.EmptyConstructor.Models;

/// <summary>
/// Source model for customer mapping
/// </summary>
public class Customer
{
  public required string Name { get; init; }
  public required string Email { get; init; }
}
