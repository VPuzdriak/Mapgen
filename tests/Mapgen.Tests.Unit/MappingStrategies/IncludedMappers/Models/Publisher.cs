namespace Mapgen.Tests.Unit.MappingStrategies.IncludedMappers.Models;

public class Publisher
{
  public required Guid Id { get; init; }
  public required string Name { get; init; }
  public required string Country { get; init; }
  public required decimal Revenue { get; init; }
}
