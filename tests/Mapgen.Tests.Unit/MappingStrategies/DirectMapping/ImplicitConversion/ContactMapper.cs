using Mapgen.Analyzer;
using Mapgen.Tests.Unit.MappingStrategies.DirectMapping.ImplicitConversion.Models;

namespace Mapgen.Tests.Unit.MappingStrategies.DirectMapping.ImplicitConversion;

[Mapper]
public partial class ContactMapper
{
  public partial ContactDto ToDto(Contact source);
}
