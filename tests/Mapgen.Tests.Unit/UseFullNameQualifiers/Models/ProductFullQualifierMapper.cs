using Mapgen.Analyzer;

using ProductDto = Mapgen.Tests.Unit.UseFullNameQualifiers.Models.Dto.Product;
using ProductEntity = Mapgen.Tests.Unit.UseFullNameQualifiers.Models.Entity.Product;

namespace Mapgen.Tests.Unit.UseFullNameQualifiers.Models;

/// <summary>
/// This mapper uses UseFullNameQualifiers = true.
/// The generated code should NOT include the alias usings above.
/// It should only have regular namespace usings like:
/// - using System;
/// - using System.Collections.Generic;
/// But NOT:
/// - using ProductDto = ...
/// - using ProductEntity = ...
/// </summary>
[Mapper(UseFullNameQualifiers = true)]
public partial class ProductFullQualifierMapper
{
  public partial ProductDto ToDto(ProductEntity entity);
}

