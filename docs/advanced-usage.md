# Advanced Usage

This guide covers advanced Mapgen features and patterns for complex mapping scenarios.

## Table of Contents
- [Mapper Composition](#mapper-composition)
- [Complex Collection Scenarios](#complex-collection-scenarios)
- [Multi-Parameter Mapping](#multi-parameter-mapping)
- [Conditional Mapping](#conditional-mapping)
- [Flattening and Unflattening](#flattening-and-unflattening)
- [Type Conversions](#type-conversions)
- [Testing Mappers](#testing-mappers)
- [Performance Optimization](#performance-optimization)

## Mapper Composition

Use `IncludeMappers()` to compose mappers together for nested object mappings.

### Basic Composition

```csharp
[Mapper]
public partial class AddressMapper
{
    public partial AddressDto ToDto(Address source);
}

[Mapper]
public partial class PersonMapper
{
    public partial PersonDto ToDto(Person source);

    public PersonMapper()
    {
        // Include other mappers for nested objects
        IncludeMappers([new AddressMapper()]);
    }
}
```

### Multiple Mapper Composition

```csharp
[Mapper]
public partial class OrderMapper
{
    public partial OrderDto ToDto(Order source);

    public OrderMapper()
    {
        IncludeMappers([
            new CustomerMapper(),
            new ProductMapper(),
            new AddressMapper()
        ]);
    }
}
```

### Composition Benefits

- Reuse mapping logic across multiple mappers
- Maintain single responsibility for each mapper
- Easier testing and maintenance
- Clear dependencies between mappers

## Complex Collection Scenarios

### Collection with Additional Parameters

```csharp
[Mapper]
public partial class ShoppingCartMapper
{
    public partial ShoppingCartDto ToDto(ShoppingCart cart, User user, Discount discount);

    public ShoppingCartMapper()
    {
        MapCollection<CartItemDto, CartItem>(
            dest => dest.Items,
            (item, cart, user, discount) => 
                item.ToDto(user, discount)
        );
    }
}
```

### Custom Source Collection

```csharp
[Mapper]
public partial class ReportMapper
{
    public partial ReportDto ToDto(Report source, DateTime startDate, DateTime endDate);

    public ReportMapper()
    {
        MapCollection<TransactionDto, Transaction>(
            dest => dest.Transactions,
            report => report.AllTransactions.Where(t => 
                t.Date >= startDate && t.Date <= endDate),
            (transaction, report, startDate, endDate) => 
                transaction.ToDto()
        );
    }
}
```

### Collection Type Conversions

```csharp
public class Source
{
    public List<Item> Items { get; set; }
}

public class Destination
{
    public ImmutableList<ItemDto> Items { get; set; }
}

[Mapper]
public partial class SourceMapper
{
    public partial Destination ToDto(Source source);

    public SourceMapper()
    {
        MapCollection<ItemDto, Item>(
            dest => dest.Items,
            item => item.ToDto()
        );
    }
}
```

### Filtering Collections During Mapping

```csharp
[Mapper]
public partial class ProductCatalogMapper
{
    public partial ProductCatalogDto ToDto(ProductCatalog source);

    public ProductCatalogMapper()
    {
        // Map only active products
        MapCollection<ProductDto, Product>(
            dest => dest.Products,
            catalog => catalog.Products.Where(p => p.IsActive),
            product => product.ToDto()
        );
    }
}
```

## Multi-Parameter Mapping

### Three Parameters

```csharp
[Mapper]
public partial class InvoiceMapper
{
    public partial InvoiceDto ToDto(Invoice invoice, Customer customer, TaxRate taxRate);

    public InvoiceMapper()
    {
        MapMember(dto => dto.CustomerName, 
                  (invoice, customer, _) => customer.Name);
        
        MapMember(dto => dto.TaxAmount, 
                  (invoice, _, taxRate) => invoice.Total * taxRate.Rate);
        
        MapMember(dto => dto.GrandTotal, 
                  (invoice, _, taxRate) => 
                      invoice.Total * (1 + taxRate.Rate));
    }
}
```

### Four or More Parameters

```csharp
[Mapper]
public partial class ComplexMapper
{
    public partial ResultDto ToDto(
        DataSource source, 
        User user, 
        Settings settings, 
        Context context);

    public ComplexMapper()
    {
        MapMember(dto => dto.ProcessedValue, 
                  (source, user, settings, context) => 
                      ProcessData(source.Value, user, settings, context));
    }
    
    private static string ProcessData(
        string value, 
        User user, 
        Settings settings, 
        Context context)
    {
        // Complex processing logic
        return $"{value}-{user.Id}-{settings.Mode}-{context.Environment}";
    }
}
```

## Conditional Mapping

### Ternary Operators

```csharp
[Mapper]
public partial class UserMapper
{
    public partial UserDto ToDto(User source);

    public UserMapper()
    {
        MapMember(dto => dto.Status, 
                  user => user.IsActive ? "Active" : "Inactive");
        
        MapMember(dto => dto.DisplayName, 
                  user => string.IsNullOrEmpty(user.NickName) 
                      ? user.FullName 
                      : user.NickName);
    }
}
```

### Switch Expressions

```csharp
[Mapper]
public partial class OrderMapper
{
    public partial OrderDto ToDto(Order source);

    public OrderMapper()
    {
        MapMember(dto => dto.StatusText, user => user.Status switch
        {
            OrderStatus.Pending => "Awaiting Payment",
            OrderStatus.Paid => "Processing",
            OrderStatus.Shipped => "In Transit",
            OrderStatus.Delivered => "Completed",
            OrderStatus.Cancelled => "Cancelled",
            _ => "Unknown"
        });
        
        MapMember(dto => dto.Priority, GetPriority);
    }
    
    private static string GetPriority(Order order) => (order.Value, order.IsExpress) switch
    {
        ( > 1000, true) => "Critical",
        ( > 1000, false) => "High",
        ( > 100, true) => "High",
        ( > 100, false) => "Medium",
        (_, true) => "Medium",
        _ => "Low"
    };
}
```

### Null Coalescing

```csharp
[Mapper]
public partial class ProductMapper
{
    public partial ProductDto ToDto(Product source);

    public ProductMapper()
    {
        MapMember(dto => dto.Description, 
                  product => product.Description ?? "No description available");
        
        MapMember(dto => dto.ImageUrl, 
                  product => product.ImageUrl ?? "/images/default.png");
        
        MapMember(dto => dto.CategoryName, 
                  product => product.Category?.Name ?? "Uncategorized");
    }
}
```

## Flattening and Unflattening

### Flattening Nested Objects

```csharp
public class Order
{
    public int Id { get; set; }
    public Customer Customer { get; set; }
    public Address ShippingAddress { get; set; }
}

public class Customer
{
    public string Name { get; set; }
    public string Email { get; set; }
}

public class Address
{
    public string Street { get; set; }
    public string City { get; set; }
    public string ZipCode { get; set; }
}

public class OrderDto
{
    public int Id { get; set; }
    public string CustomerName { get; set; }
    public string CustomerEmail { get; set; }
    public string ShippingStreet { get; set; }
    public string ShippingCity { get; set; }
    public string ShippingZipCode { get; set; }
}

[Mapper]
public partial class OrderMapper
{
    public partial OrderDto ToDto(Order source);

    public OrderMapper()
    {
        MapMember(dto => dto.CustomerName, 
                  order => order.Customer.Name);
        MapMember(dto => dto.CustomerEmail, 
                  order => order.Customer.Email);
        MapMember(dto => dto.ShippingStreet, 
                  order => order.ShippingAddress.Street);
        MapMember(dto => dto.ShippingCity, 
                  order => order.ShippingAddress.City);
        MapMember(dto => dto.ShippingZipCode, 
                  order => order.ShippingAddress.ZipCode);
    }
}
```

### Unflattening (Reverse)

```csharp
[Mapper]
public partial class OrderMapper
{
    public partial OrderDto ToDto(Order source);
    public partial Order ToEntity(OrderDto source);

    public OrderMapper()
    {
        // Flattening (Entity -> DTO)
        MapMember(dto => dto.CustomerName, 
                  order => order.Customer.Name);
        
        // Unflattening would require creating nested objects
        // This is typically done in a separate reverse mapper or method
    }
}

// For unflattening, you might need custom logic:
[Mapper]
public partial class OrderDtoMapper
{
    public partial Order ToEntity(OrderDto source);

    public OrderDtoMapper()
    {
        MapMember(entity => entity.Customer, CreateCustomer);
    }
    
    private static Customer CreateCustomer(OrderDto dto)
    {
        return new Customer
        {
            Name = dto.CustomerName,
            Email = dto.CustomerEmail
        };
    }
}
```

## Type Conversions

### Enum to String

```csharp
public enum UserRole { Admin, User, Guest }

[Mapper]
public partial class UserMapper
{
    public partial UserDto ToDto(User source);

    public UserMapper()
    {
        MapMember(dto => dto.RoleText, 
                  user => user.Role.ToString());
    }
}
```

### Enum to String with Formatting

```csharp
[Mapper]
public partial class ProductMapper
{
    public partial ProductDto ToDto(Product source);

    public ProductMapper()
    {
        MapMember(dto => dto.StatusText, FormatStatus);
    }
    
    private static string FormatStatus(Product product)
    {
        return product.Status switch
        {
            ProductStatus.Available => "✓ In Stock",
            ProductStatus.LowStock => "⚠ Low Stock",
            ProductStatus.OutOfStock => "✗ Out of Stock",
            _ => "Unknown"
        };
    }
}
```

### DateTime Formatting

```csharp
[Mapper]
public partial class EventMapper
{
    public partial EventDto ToDto(Event source);

    public EventMapper()
    {
        MapMember(dto => dto.DateText, 
                  evt => evt.Date.ToString("yyyy-MM-dd"));
        
        MapMember(dto => dto.TimeText, 
                  evt => evt.Date.ToString("HH:mm"));
        
        MapMember(dto => dto.FullDateTimeText, 
                  evt => evt.Date.ToString("f")); // Full date/time pattern
    }
}
```

### Custom Type Conversions

```csharp
public class Money
{
    public decimal Amount { get; set; }
    public string Currency { get; set; }
}

[Mapper]
public partial class OrderMapper
{
    public partial OrderDto ToDto(Order source);

    public OrderMapper()
    {
        MapMember(dto => dto.TotalPrice, ConvertMoney);
    }
    
    private static string ConvertMoney(Order order)
    {
        var money = order.Total;
        var symbol = money.Currency switch
        {
            "USD" => "$",
            "EUR" => "€",
            "GBP" => "£",
            _ => money.Currency
        };
        return $"{symbol}{money.Amount:N2}";
    }
}
```

## Testing Mappers

### Unit Testing

```csharp
using Xunit;

public class UserMapperTests
{
    [Fact]
    public void ToDto_MapsBasicProperties()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com"
        };
        
        var mapper = new UserMapper();
        
        // Act
        var dto = mapper.ToDto(user);
        
        // Assert
        Assert.Equal(user.Id, dto.Id);
        Assert.Equal("John Doe", dto.FullName);
        Assert.Equal(user.Email, dto.Email);
    }
    
    [Fact]
    public void ToDto_HandlesNullValues()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            FirstName = "John",
            LastName = null,
            Email = null
        };
        
        var mapper = new UserMapper();
        
        // Act
        var dto = mapper.ToDto(user);
        
        // Assert
        Assert.NotNull(dto);
        Assert.Equal("No email", dto.Email);
    }
}
```

### Integration Testing

```csharp
public class OrderMappingIntegrationTests
{
    [Fact]
    public void CompleteOrderMapping_WorksEndToEnd()
    {
        // Arrange
        var order = CreateComplexOrder();
        var customer = CreateCustomer();
        var settings = CreateSettings();
        
        var mapper = new OrderMapper();
        
        // Act
        var dto = mapper.ToDto(order, customer, settings);
        
        // Assert
        Assert.NotNull(dto);
        Assert.Equal(order.Id, dto.Id);
        Assert.Equal(customer.Name, dto.CustomerName);
        Assert.NotEmpty(dto.Items);
    }
}
```

## Performance Optimization

### Static Methods

Use static methods when possible for better performance:

```csharp
[Mapper]
public partial class UserMapper
{
    public partial UserDto ToDto(User source);

    public UserMapper()
    {
        MapMember(dto => dto.FullName, GetFullName);
    }
    
    // Static = better performance (no instance needed)
    private static string GetFullName(User user) 
        => $"{user.FirstName} {user.LastName}";
}
```

### Reuse Mapper Instances

```csharp

// ✅ Good - cache if needed
private static readonly UserMapper _userMapper = new();

// ⚠️ Less efficient - creates new instance every time
public UserDto Convert(User user) => new UserMapper().ToDto(user);

// ✅ Better - use extension method (creates once internally)
public UserDto Convert(User user) => user.ToDto();
```

## Next Steps

- Review [Best Practices](migration/best-practices.md)
- Compare with [other mapping libraries](comparison.md)
