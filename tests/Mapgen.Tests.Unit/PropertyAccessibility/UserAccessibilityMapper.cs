using Mapgen.Analyzer;
using Mapgen.Tests.Unit.PropertyAccessibility.Models;

namespace Mapgen.Tests.Unit.PropertyAccessibility;

[Mapper]
public partial class UserAccessibilityMapper
{
  public partial UserDto ToDto(User user);
}
