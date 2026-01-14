using Mapgen.Analyzer;
using Mapgen.Tests.Unit.PropertyAccessibility.Models;

namespace Mapgen.Tests.Unit.PropertyAccessibility;

[Mapper]
public partial class ProductAccessibilityMapper
{
  public partial ProductDto ToDto(Product product);
}
