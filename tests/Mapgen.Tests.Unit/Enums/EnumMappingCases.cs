using FluentAssertions;

using Mapgen.Tests.Unit.Enums.Models.Contracts.Enums;
using Mapgen.Tests.Unit.Enums.Models.Entity;

namespace Mapgen.Tests.Unit.Enums;

public class EnumMappingCases
{
  [Fact]
  public void When_EnumProperties_ShouldMapCorrectly()
  {
    // Arrange
    var bill = new Bill
    {
      Id = 1001,
      Status = BillStatus.Paid,
      PaymentMethod = PaymentType.CreditCard,
      StatusTransitions = [BillStatus.Draft, BillStatus.Sent, BillStatus.Paid]
    };

    var mapper = new BillMapper();

    // Act
    var result = mapper.ToDto(bill);

    // Assert
    result.Id.Should().Be(bill.Id);
    result.Status.Should().Be(BillStatusDto.Paid);
    result.PaymentMethod.Should().Be(PaymentTypeDto.CreditCard);
    result.StatusTransitions.Should().BeEquivalentTo([BillStatusDto.Draft, BillStatusDto.Sent, BillStatusDto.Paid]);
  }

  [Fact]
  public void When_EnumFields_ShouldMapCorrectly()
  {
    // Arrange
    var item = new Item
    {
      Id = 42,
      Name = "Laptop",
      Category = ItemCategory.Electronics,
      Availability = ItemAvailability.InStock
    };

    var mapper = new ItemMapper();

    // Act
    var result = mapper.ToDto(item);

    // Assert
    result.Id.Should().Be(item.Id);
    result.Name.Should().Be(item.Name);
    result.Category.Should().Be(ItemCategoryDto.Electronics);
    result.Availability.Should().Be(ItemAvailabilityDto.InStock);
  }

  [Fact]
  public void When_CtorArgumentsAreEnum_ShouldMapCorrectly()
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

    var customer = new Customer { Id = 1, Status = CustomerStatus.Vip };

    var mapper = new OrderMapper();

    // Act
    var result = mapper.ToDto(order, customer);

    // Assert
    result.Id.Should().Be(order.Id);
    result.CustomerId.Should().Be(order.CustomerId);
    result.IsVipOrder.Should().BeTrue();
    result.CurrentStatus.Should().Be(OrderStatusDto.Delivered);
    result.StatusHistory.Should().BeEquivalentTo([OrderStatusDto.Pending, OrderStatusDto.Shipped, OrderStatusDto.Delivered]);
  }
}
