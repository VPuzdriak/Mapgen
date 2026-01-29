using System;

namespace Mapgen.Tests.Unit.MappingStrategies.CollectionMapping.Models;

public record MemberDto
{
  public required Guid Id { get; init; }
  public required string Name { get; init; }
  public required LeadDto Lead { get; set; }
}
