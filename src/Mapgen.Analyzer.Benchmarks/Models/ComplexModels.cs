namespace Mapgen.Analyzer.Benchmarks.Models;

/// <summary>
/// Nested entity with child objects - for complex mapping benchmark
/// </summary>
public class ComplexEntity
{
  public int Id { get; set; }
  public string Name { get; set; } = string.Empty;
  public Address Address { get; set; } = new();
  public Contact Contact { get; set; } = new();
  public List<OrderItem> Items { get; set; } = new();
  public DateTime CreatedAt { get; set; }
  public decimal TotalAmount { get; set; }
}

public class Address
{
  public string Street { get; set; } = string.Empty;
  public string City { get; set; } = string.Empty;
  public string ZipCode { get; set; } = string.Empty;
  public string Country { get; set; } = string.Empty;
}

public class Contact
{
  public string Email { get; set; } = string.Empty;
  public string Phone { get; set; } = string.Empty;
}

public class OrderItem
{
  public int ProductId { get; set; }
  public string ProductName { get; set; } = string.Empty;
  public int Quantity { get; set; }
  public decimal UnitPrice { get; set; }
}

public class ComplexDto
{
  public int Id { get; set; }
  public string Name { get; set; } = string.Empty;
  public AddressDto Address { get; set; } = new();
  public ContactDto Contact { get; set; } = new();
  public List<OrderItemDto> Items { get; set; } = new();
  public DateTime CreatedAt { get; set; }
  public decimal TotalAmount { get; set; }
}

public class AddressDto
{
  public string Street { get; set; } = string.Empty;
  public string City { get; set; } = string.Empty;
  public string ZipCode { get; set; } = string.Empty;
  public string Country { get; set; } = string.Empty;
}

public class ContactDto
{
  public string Email { get; set; } = string.Empty;
  public string Phone { get; set; } = string.Empty;
}

public class OrderItemDto
{
  public int ProductId { get; set; }
  public string ProductName { get; set; } = string.Empty;
  public int Quantity { get; set; }
  public decimal UnitPrice { get; set; }
}
