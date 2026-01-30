using Mapgen.Analyzer;

using PersonContract = Mapgen.Tests.Unit.FullNameTypes.Models.Contract.Person;
using PersonEntity = Mapgen.Tests.Unit.FullNameTypes.Models.Entity.Person;

namespace Mapgen.Tests.Unit.FullNameTypes;

[Mapper]
public partial class PersonFullNameMapper
{
  public partial PersonContract ToContract(PersonEntity person, Models.Entity.Person? partner);

  public PersonFullNameMapper()
  {
    MapMember(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}");
    MapMember(dest => dest.Partner, (_, partner) => partner is null ? null : ToContract(partner, null));
  }
}
