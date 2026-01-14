using Mapgen.Analyzer.Abstractions;
using Mapgen.Tests.Unit.CollectionMapping.Models;

namespace Mapgen.Tests.Unit.CollectionMapping;

[Mapper]
public partial class MemberMapper
{
  public partial MemberDto ToDto(Member source, Lead lead);

  public MemberMapper()
  {
    MapMember(dest => dest.Lead, (_, lead) => lead.ToDto());
  }
}
