namespace Mapgen.Tests.Unit.PropertyAccessibility.Models;

/// <summary>
/// Product entity with calculated fields and encapsulation
/// </summary>
public class Product
{
  public required string Sku { get; init; }
  public required string Name { get; set; }
  public required decimal BasePrice { get; set; }
  public required decimal TaxRate { get; set; }

  // Calculated property - computed from BasePrice and TaxRate
  public decimal TotalPrice => BasePrice * (1 + TaxRate);

  // Discount can only be set internally (business rule enforcement)
  public decimal DiscountPercentage { get; private set; }

  // Stock level can only be read from outside (write operations through methods)
  public int StockLevel { get; private set; }

  public void AddStock(int quantity) => StockLevel += quantity;
  public void ApplyDiscount(decimal percentage) => DiscountPercentage = percentage;
}
