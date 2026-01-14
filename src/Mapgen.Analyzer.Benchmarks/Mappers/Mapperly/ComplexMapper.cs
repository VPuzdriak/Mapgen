using Mapgen.Analyzer.Benchmarks.Models;

namespace Mapgen.Analyzer.Benchmarks.Mappers.Mapperly;

[Riok.Mapperly.Abstractions.Mapper]
public partial class ComplexMapper
{
  public partial ComplexDto ToDto(ComplexEntity entity);

  // Mapperly will automatically map nested objects and collections
  private partial AddressDto MapAddress(Address address);
  private partial ContactDto MapContact(Contact contact);
  private partial OrderItemDto MapOrderItem(OrderItem item);
}
