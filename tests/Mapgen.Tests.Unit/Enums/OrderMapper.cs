using Mapgen.Analyzer;
using Mapgen.Tests.Unit.Enums.Models.Contracts;
using Mapgen.Tests.Unit.Enums.Models.Contracts.Enums;
using Mapgen.Tests.Unit.Enums.Models.Entity;

using CustomerStatusContract = Mapgen.Tests.Unit.Enums.Models.Contracts.Enums.CustomerStatus;

namespace Mapgen.Tests.Unit.Enums;

[Mapper]
public partial class OrderMapper
{
  public partial OrderDto ToDto(Order order, Customer customer);

  public OrderMapper()
  {
    UseConstructor(
      (src, customer) => src.Id,
      (src, customer) => src.CustomerId,
      (src, customer) => MapToOrderPriorityDto(src.OrderPriority),
      (src, customer) => MapToCustomerStatusDto(customer.Status)
    );
  }

  private CustomerStatusContract MapToCustomerStatusDto(Models.Entity.CustomerStatus customerStatus)
  {
    return customerStatus switch
    {
      Models.Entity.CustomerStatus.Regular => CustomerStatusContract.Regular,
      Models.Entity.CustomerStatus.Vip => CustomerStatusContract.Vip,
      _ => CustomerStatusContract.Regular
    };
  }
}
