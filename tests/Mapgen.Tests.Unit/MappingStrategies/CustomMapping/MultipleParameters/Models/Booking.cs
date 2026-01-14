namespace Mapgen.Tests.Unit.MappingStrategies.CustomMapping.MultipleParameters.Models;

public class Booking
{
  public required Guid Id { get; init; }
  public required DateOnly CheckInDate { get; init; }
}
