using Mapgen.Analyzer;
using Mapgen.Tests.Unit.PropertyAccessibility.Models;

namespace Mapgen.Tests.Unit.PropertyAccessibility;

[Mapper]
public partial class OrderAccessibilityMapper
{
  public partial OrderDto ToDto(Order order);
}
