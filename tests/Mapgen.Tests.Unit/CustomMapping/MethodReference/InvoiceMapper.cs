using System.Globalization;

using Mapgen.Analyzer.Abstractions;
using Mapgen.Tests.Unit.CustomMapping.MethodReference.Models;

namespace Mapgen.Tests.Unit.CustomMapping.MethodReference;

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
