using System.Globalization;

namespace Mapgen.Tests.Unit.PropertyAccessibility.Models;

/// <summary>
/// DTO for displaying product information to customers
/// </summary>
public class ProductDto
{
  public required string Sku { get; init; }
  public required string Name { get; set; }
  public required decimal BasePrice { get; set; }
  public required decimal TaxRate { get; set; }

  // These can be set in DTO (for display purposes)
  public decimal TotalPrice { get; set; }
  public int StockLevel { get; set; }

  // Display-only field, cannot be mapped to
  public string FormattedPrice => $"${TotalPrice.ToString(new CultureInfo("nl-NL"))}";
}
