using System;

namespace Mapgen.Tests.Unit.MappingStrategies.CustomMapping.MultipleParameters.Models;

public class Shipment
{
  public required Guid Id { get; init; }
  public required string TrackingNumber { get; init; }
}
