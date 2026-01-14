namespace Mapgen.Tests.Unit.MappingStrategies.IncludedMappers.Models;

public class GamingStudioDto
{
  public required Guid Id { get; init; }
  public required string Name { get; init; }
  public required int FoundedYear { get; init; }
  public required PublisherDto Publisher { get; init; }
}
