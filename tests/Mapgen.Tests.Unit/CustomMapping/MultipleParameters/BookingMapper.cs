using Mapgen.Analyzer.Abstractions;
using Mapgen.Tests.Unit.CustomMapping.MultipleParameters.Models;

namespace Mapgen.Tests.Unit.CustomMapping.MultipleParameters;

[Mapper]
public partial class BookingMapper
{
  public partial BookingDto ToDto(Booking booking, Guest guest);

  public BookingMapper()
  {
    MapMember(dto => dto.BookingId, booking => booking.Id);
    MapMember(dto => dto.GuestName, (_, guest) => guest.Name);
    MapMember(dto => dto.GuestEmail, (_, guest) => guest.Email);
  }
}
