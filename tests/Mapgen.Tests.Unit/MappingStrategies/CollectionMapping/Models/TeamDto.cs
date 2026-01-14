namespace Mapgen.Tests.Unit.MappingStrategies.CollectionMapping.Models;

public class TeamDto
{
  public required Guid Id { get; init; }
  public required string Name { get; init; }
  public required List<MemberDto> Members { get; init; }
}
