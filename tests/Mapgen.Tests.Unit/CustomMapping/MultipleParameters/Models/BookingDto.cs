namespace Mapgen.Tests.Unit.CustomMapping.MultipleParameters.Models;

public class BookingDto
{
  public required Guid BookingId { get; init; }
  public required DateOnly CheckInDate { get; init; }
  public required string GuestName { get; init; }
  public required string GuestEmail { get; init; }
}
