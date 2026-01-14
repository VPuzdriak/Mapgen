using FluentAssertions;

using Mapgen.Tests.Unit.MappingStrategies.CustomMapping.MultipleParameters.Models;

namespace Mapgen.Tests.Unit.MappingStrategies.CustomMapping.MultipleParameters;

public class MultipleParametersCases
{
  [Fact]
  public void When_MapperWithTwoParameters_MapSuccessfully()
  {
    // Arrange
    var booking = new Booking { Id = Guid.NewGuid(), CheckInDate = new DateOnly(2026, 3, 15) };
    var guest = new Guest { Name = "Alice Johnson", Email = "alice@example.com" };

    // Act
    var result = booking.ToDto(guest);

    // Assert
    result.BookingId.Should().Be(booking.Id);
    result.GuestName.Should().Be(guest.Name);
    result.GuestEmail.Should().Be(guest.Email);
  }

  [Fact]
  public void When_MapperWithThreeParameters_MapSuccessfully()
  {
    // Arrange
    var shipment = new Shipment { Id = Guid.NewGuid(), TrackingNumber = "SHIP123" };
    var sender = new Party { Name = "Sender Inc" };
    var receiver = new Party { Name = "Receiver Ltd" };

    // Act
    var result = shipment.ToDto(sender, receiver);

    // Assert
    result.ShipmentId.Should().Be(shipment.Id);
    result.TrackingNumber.Should().Be(shipment.TrackingNumber);
    result.SenderName.Should().Be(sender.Name);
    result.ReceiverName.Should().Be(receiver.Name);
  }
}
