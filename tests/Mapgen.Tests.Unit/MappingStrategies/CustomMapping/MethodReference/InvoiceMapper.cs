using System.Globalization;

using Mapgen.Analyzer;
using Mapgen.Tests.Unit.MappingStrategies.CustomMapping.MethodReference.Models;

namespace Mapgen.Tests.Unit.MappingStrategies.CustomMapping.MethodReference;

[Mapper]
public partial class InvoiceMapper
{
  public partial InvoiceDto ToDto(Invoice source);

  public InvoiceMapper()
  {
    MapMember(dto => dto.Number, source => source.InvoiceNumber);
    MapMember(dto => dto.FormattedAmount, FormatAmount);
  }

  private string FormatAmount(Invoice source) => "€" + source.Amount.ToString(new CultureInfo("nl-NL"));
}
