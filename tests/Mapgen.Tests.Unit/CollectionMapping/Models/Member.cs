namespace Mapgen.Tests.Unit.CollectionMapping.Models;

public record Member
{
  public required Guid Id { get; init; }
  public required string Name { get; init; }
}
