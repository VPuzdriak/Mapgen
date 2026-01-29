using System;

namespace Mapgen.Tests.Unit.MappingStrategies.IncludedMappers.Models;

public class GamingStudio
{
  public required Guid Id { get; init; }
  public required string Name { get; init; }
  public required int FoundedYear { get; init; }
  public required Publisher Publisher { get; init; }
}
