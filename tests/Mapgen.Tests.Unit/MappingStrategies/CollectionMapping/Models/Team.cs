using System;
using System.Collections.Generic;

namespace Mapgen.Tests.Unit.MappingStrategies.CollectionMapping.Models;

public class Team
{
  public required Guid Id { get; init; }
  public required string Name { get; init; }
  public required List<Member> Members { get; init; }
}
