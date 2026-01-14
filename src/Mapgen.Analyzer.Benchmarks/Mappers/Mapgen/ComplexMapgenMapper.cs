using Mapgen.Analyzer.Benchmarks.Models;

namespace Mapgen.Analyzer.Benchmarks.Mappers.Mapgen;

[Mapper]
public partial class AddressMapgenMapper
{
  public partial AddressDto ToDto(Address entity);
}

[Mapper]
public partial class ContactMapgenMapper
{
  public partial ContactDto ToDto(Contact entity);
}

[Mapper]
public partial class OrderItemMapgenMapper
{
  public partial OrderItemDto ToDto(OrderItem entity);
}

[Mapper]
public partial class ComplexMapgenMapper
{
  public partial ComplexDto ToDto(ComplexEntity entity);

  public ComplexMapgenMapper()
  {
    IncludeMappers([new AddressMapgenMapper(), new ContactMapgenMapper(), new OrderItemMapgenMapper()]);
  }
}
