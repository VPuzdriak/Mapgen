using FluentAssertions;

using Mapgen.Tests.Unit.Enums.Models.Contracts;
using Mapgen.Tests.Unit.Enums.Models.Contracts.Enums;
using Mapgen.Tests.Unit.Enums.Models.Entity;

namespace Mapgen.Tests.Unit.Enums;

public class EnumMappingCases
{
  [Fact]
  public void When_EnumProperties_ShouldMapCorrectly()
  {
    // Arrange
    var order = new Order
    {
      Id = 1,
      CustomerId = 100,
      OrderPriority = OrderPriority.High,
      CurrentStatus = OrderStatus.Delivered,
      StatusHistory = [OrderStatus.Pending, OrderStatus.Shipped, OrderStatus.Delivered]
    };
    var mapper = new OrderMapper();

    // Act
    var result = mapper.ToDto(order);

    // Assert
    result.Id.Should().Be(order.Id);
    result.CustomerId.Should().Be(order.CustomerId);
    result.IsVipOrder.Should().BeTrue();
    result.CurrentStatus.Should().Be(OrderStatusDto.Delivered);
    result.StatusHistory.Should().BeEquivalentTo([OrderStatusDto.Pending, OrderStatusDto.Shipped, OrderStatusDto.Delivered]);
  }
}
