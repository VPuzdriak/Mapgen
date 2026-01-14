using Mapgen.Analyzer;
using Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.EmptyConstructor.Models;

namespace Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.EmptyConstructor;

/// <summary>
/// Mapper using parameterized constructor (explicit selection)
/// </summary>
[Mapper]
public partial class CustomerMapperWithConstructor
{
  public partial CustomerDto ToDto(Customer source);

  public CustomerMapperWithConstructor()
  {
    UseConstructor(
      source => source.Name,
      source => source.Email
    );
  }
}
