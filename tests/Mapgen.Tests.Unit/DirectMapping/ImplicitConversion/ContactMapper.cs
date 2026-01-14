using Mapgen.Analyzer.Abstractions;
using Mapgen.Tests.Unit.DirectMapping.ImplicitConversion.Models;

namespace Mapgen.Tests.Unit.DirectMapping.ImplicitConversion;

[Mapper]
public partial class ContactMapper
{
  public partial ContactDto ToDto(Contact source);
}
