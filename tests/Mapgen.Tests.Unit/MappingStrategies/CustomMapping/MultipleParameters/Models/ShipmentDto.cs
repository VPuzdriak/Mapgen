namespace Mapgen.Tests.Unit.MappingStrategies.CustomMapping.MultipleParameters.Models;

public class ShipmentDto
{
  public required Guid ShipmentId { get; init; }
  public required string TrackingNumber { get; init; }
  public required string SenderName { get; init; }
  public required string ReceiverName { get; init; }
}
