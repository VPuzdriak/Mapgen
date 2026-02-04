
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.AutoConstructor.Models;

public class ShopDto
{
  public ImmutableList<ProductDto> Products { get; }

  public ShopDto(ImmutableList<ProductDto> products)
  {
    Products = products;
  }
}
