using Mapgen.Analyzer;
using Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.AutoConstructor.Models;

namespace Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.AutoConstructor;

/// <summary>
/// Mapper that tests implicit type conversion in auto-constructor mapping
/// </summary>
[Mapper]
public partial class AutoPersonMapper
{
  public partial PersonDto ToDto(Person source);

  // No constructor configuration - should auto-detect and use constructor with implicit conversion (int -> long)
}
