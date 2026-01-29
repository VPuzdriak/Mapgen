using System;

namespace Mapgen.Tests.Unit.MappingStrategies.CollectionMapping.Models;

public class Lead
{
  public required Guid Guid { get; init; }
  public required string Name { get; init; }
}
