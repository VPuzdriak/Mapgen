using Mapgen.Analyzer;

using PersonContract = Mapgen.Tests.Unit.Aliasing.Models.Contract.Person;
using PersonEntity = Mapgen.Tests.Unit.Aliasing.Models.Entity.Person;

namespace Mapgen.Tests.Unit.Aliasing.Models;

[Mapper]
public partial class PersonAliasMapper
{
  public partial PersonContract ToContract(PersonEntity person, PersonEntity? partner);

  public PersonAliasMapper()
  {
    MapMember(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}");
    MapMember(dest => dest.Partner, (_, partner) => partner is null ? null : ToContract(partner, null));
  }
}
