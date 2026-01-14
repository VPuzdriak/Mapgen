using Mapgen.Analyzer;
using Mapgen.Tests.Unit.MappingStrategies.CollectionMapping.Models;

namespace Mapgen.Tests.Unit.MappingStrategies.CollectionMapping;

[Mapper]
public partial class MemberMapper
{
  public partial MemberDto ToDto(Member source, Lead lead);

  public MemberMapper()
  {
    MapMember(dest => dest.Lead, (_, lead) => lead.ToDto());
  }
}
