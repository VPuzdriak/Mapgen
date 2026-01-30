using Mapgen.Analyzer;

namespace Mapgen.Tests.Unit.FieldsMapping;

[Mapper]
public partial class PublicFieldsMapper
{
  public partial Models.Contracts.PersonDto ToContract(Models.Entities.Person source);

  public PublicFieldsMapper()
  {
    MapMember(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}");
  }
}
