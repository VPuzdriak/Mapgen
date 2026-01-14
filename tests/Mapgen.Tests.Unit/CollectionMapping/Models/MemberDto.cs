namespace Mapgen.Tests.Unit.CollectionMapping.Models;

public record MemberDto
{
  public required Guid Id { get; init; }
  public required string Name { get; init; }
  public required LeadDto Lead { get; set; }
}
