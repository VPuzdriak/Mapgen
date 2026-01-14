using Mapgen.Analyzer;
using Mapgen.Tests.Unit.MappingStrategies.CustomMapping.MultipleParameters.Models;

namespace Mapgen.Tests.Unit.MappingStrategies.CustomMapping.MultipleParameters;

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
