using Mapgen.Analyzer.Benchmarks.Models;

using Mapster;

namespace Mapgen.Analyzer.Benchmarks.Mappers.Mapster;

public static class ComplexMapperConfig
{
  public static void Configure()
  {
    // Complex mapping - Mapster auto-maps nested objects and collections
    TypeAdapterConfig<Address, AddressDto>.NewConfig();
    TypeAdapterConfig<Contact, ContactDto>.NewConfig();
    TypeAdapterConfig<OrderItem, OrderItemDto>.NewConfig();
    TypeAdapterConfig<ComplexEntity, ComplexDto>.NewConfig();
  }
}
