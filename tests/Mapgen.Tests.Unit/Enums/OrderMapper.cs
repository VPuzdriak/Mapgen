using Mapgen.Analyzer;
using Mapgen.Tests.Unit.Enums.Models.Contracts;
using Mapgen.Tests.Unit.Enums.Models.Contracts.Enums;
using Mapgen.Tests.Unit.Enums.Models.Entity;

namespace Mapgen.Tests.Unit.Enums;

[Mapper]
public partial class OrderMapper
{
  public partial OrderDto ToDto(Order order, Customer customer);

  public OrderMapper()
  {
    /*
     * CustomerStatusDto is not mapped from source member, so we need to map it manually
     * Or we can use MapEnum to map it automatically
     * This generates MapToCustomerStatusDto method which maps CustomerStatus to CustomerStatusDto
     */
    MapEnum<CustomerStatus, CustomerStatusDto>();

    UseConstructor(
      (src, customer) => src.Id,
      (src, customer) => src.CustomerId,
      // OrderPriority is a ctor argument and exists in source, so MapToOrderPriorityDto is generated automatically
      (src, customer) => MapToOrderPriorityDto(src.OrderPriority),
      (src, customer) => MapToCustomerStatusDto(customer.Status)
    );
  }
}
