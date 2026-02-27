using System.Collections.Immutable;

using Mapgen.Tests.Unit.Enums.Models.Contracts.Enums;

namespace Mapgen.Tests.Unit.Enums.Models.Contracts;

public class BillDto
{
  public required int Id { get; init; }
  public required BillStatusDto Status { get; init; }
  public required PaymentTypeDto? PaymentMethod { get; init; }
  public required IImmutableList<BillStatusDto> StatusTransitions { get; init; } = [];
}

