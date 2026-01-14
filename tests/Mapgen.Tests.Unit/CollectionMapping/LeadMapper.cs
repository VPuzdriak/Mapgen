using Mapgen.Analyzer.Abstractions;
using Mapgen.Tests.Unit.CollectionMapping.Models;

namespace Mapgen.Tests.Unit.CollectionMapping;

[Mapper]
public partial class LeadMapper
{
  public partial LeadDto ToDto(Lead source);
}
