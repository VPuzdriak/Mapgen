using Mapgen.Analyzer;
using Mapgen.Tests.Unit.MappingStrategies.CollectionMapping.Models;

namespace Mapgen.Tests.Unit.MappingStrategies.CollectionMapping;

[Mapper]
public partial class LeadMapper
{
  public partial LeadDto ToDto(Lead source);
}
