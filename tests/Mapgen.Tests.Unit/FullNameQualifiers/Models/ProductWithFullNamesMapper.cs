using Mapgen.Analyzer;

using ProductContract = Mapgen.Tests.Unit.FullNameQualifiers.Models.Contract.Product;
using ProductEntity = Mapgen.Tests.Unit.FullNameQualifiers.Models.Entity.Product;

namespace Mapgen.Tests.Unit.FullNameQualifiers.Models;

[Mapper(UseFullNameQualifiers = true)]
public partial class ProductWithFullNamesMapper
{
  public partial ProductContract ToContract(ProductEntity product);

  public ProductWithFullNamesMapper()
  {
    MapMember(dest => dest.Name, src => src.ProductName);
    MapMember(dest => dest.Price, src => src.ProductPrice);
  }
}

