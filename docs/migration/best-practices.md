# Best Practices

This guide covers best practices for using Mapgen effectively in your projects.

## Table of Contents
- [Mapper Design](#mapper-design)
- [Naming Conventions](#naming-conventions)
- [Documentation](#documentation)
- [Performance Optimization](#performance-optimization)
- [Testing](#testing)
- [Code Organization](#code-organization)

## Mapper Design

### Keep Mappers Focused

Each mapper should handle one type of mapping. This follows the Single Responsibility Principle and makes your code easier to maintain and test.

```csharp
// ✅ Good - focused
[Mapper]
public partial class UserMapper
{
    public partial UserDto ToDto(User source);
}

// ❌ Can't be - too many responsibilities. Compile-time error
// Mapping class can have only one mapping method
[Mapper]
public partial class EverythingMapper
{
    public partial UserDto ToUserDto(User source);
    public partial ProductDto ToProductDto(Product source);
    public partial OrderDto ToOrderDto(Order source);
}
```

**Why this matters:**
- Easier to test individual mappers
- Clear separation of concerns
- Better code organization
- Simpler debugging

### Use Mapper Composition

For nested objects, use `IncludeMappers()` to compose mappers together rather than handling all mapping logic in one place.

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

**Benefits:**
- Reuse mapping logic across multiple mappers
- Maintain single responsibility for each mapper
- Easier testing and maintenance
- Clear dependencies between mappers

## Naming Conventions

### Use Descriptive Method Names

Choose method names that clearly indicate what the mapping produces. The standard convention is `To{DestinationType}`.

```csharp
// ✅ Good
public partial UserDto ToDto(User source);
public partial UserSummaryDto ToSummaryDto(User source);
public partial UserDetailDto ToDetailDto(User source);

// ❌ Avoid
public partial UserDto Map(User source);
public partial UserDto Convert(User source);
```

**Why this matters:**
- Immediately clear what the method does
- Consistent with common mapping conventions
- Better IntelliSense discoverability
- Self-documenting code

### Consistent Naming Patterns

Within your project, establish and follow consistent naming patterns:

```csharp
// Entity to DTO
public partial UserDto ToDto(User entity);

// DTO to Entity
public partial User ToEntity(UserDto dto);

// Entity to ViewModel
public partial UserViewModel ToViewModel(User entity);
```

## Documentation

### Document Complex Mappings

When mapping logic is not immediately obvious, add XML documentation or comments to explain the reasoning.
We don't encourage you to have complex mapping logic, but if you do, document it.

```csharp
[Mapper]
public partial class PricingMapper
{
    public partial PriceDto ToDto(Product product, Customer customer, Discount discount);

    public PricingMapper()
    {
        // Calculate final price based on customer tier and active discounts
        MapMember(dto => dto.FinalPrice, CalculateFinalPrice);
    }
    
    /// <summary>
    /// Calculates the final price considering:
    /// - Base product price
    /// - Customer tier discount (Gold: 10%, Silver: 5%)
    /// - Active promotional discounts
    /// </summary>
    private static decimal CalculateFinalPrice(
        Product product, 
        Customer customer, 
        Discount discount)
    {
        var basePrice = product.Price;
        var tierDiscount = customer.Tier switch
        {
            CustomerTier.Gold => 0.10m,
            CustomerTier.Silver => 0.05m,
            _ => 0m
        };
        var discountedPrice = basePrice * (1 - tierDiscount);
        return discountedPrice * (1 - discount.Percentage);
    }
}
```

## Performance Optimization

### Use Static Methods

Static methods perform better because they don't require an instance context. Use them whenever possible for helper methods.

```csharp
[Mapper]
public partial class UserMapper
{
    public partial UserDto ToDto(User source);

    public UserMapper()
    {
        MapMember(dto => dto.FullName, GetFullName);
    }
    
    // ✅ Static = better performance (no instance needed)
    private static string GetFullName(User user) 
        => $"{user.FirstName} {user.LastName}";
    
    // ❌ Instance method = unnecessary overhead
    private string GetFullNameInstance(User user) 
        => $"{user.FirstName} {user.LastName}";
}
```

### Reuse Mapper Instances

Mappers are stateless after construction, so reuse them instead of creating new instances repeatedly.

```csharp
// ✅ Good
static UserMapper userMapper = new UserMapper();

// Leverage the reused instance
var userDto = userMapper.ToDto(user);
var anotherDto = userMapper.ToDto(anotherUser);
```

### Prefer Extension Methods

Use the auto-generated extension methods for the cleanest syntax and best performance:

```csharp
// ✅ Best - uses cached mapper instance
var dto = user.ToDto();

// ⚠️ Avoid - creates new instance each time
var dto = new UserMapper().ToDto(user);
```

## Testing

### Unit Test Individual Mappers

Write focused unit tests for each mapper to ensure correctness:

```csharp
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
        
        // Act
        var dto = user.ToDto();
        
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
        
        // Act
        var dto = user.ToDto();
        
        // Assert
        Assert.NotNull(dto);
        Assert.Equal("John", dto.FullName);
    }
}
```

### Test Edge Cases

Always test boundary conditions and edge cases:

```csharp
[Theory]
[InlineData(null)]
[InlineData("")]
[InlineData("   ")]
public void ToDto_HandlesInvalidStrings(string input)
{
    var product = new Product { Description = input };
    var mapper = new ProductMapper();
    
    var dto = mapper.ToDto(product);
    
    Assert.Equal("No description available", dto.Description);
}
```

### Integration Testing

Test mappers with composed dependencies:

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
        
        // Act
        var dto = order.ToDto(customer, settings);
        
        // Assert
        Assert.NotNull(dto);
        Assert.Equal(order.Id, dto.Id);
        Assert.Equal(customer.Name, dto.CustomerName);
        Assert.NotEmpty(dto.Items);
    }
}
```

## Code Organization

### Organize Mappers by Domain

Group related mappers together:

```
YourProject/
├── Mappers/
│   ├── Users/
│   │   ├── UserMapper.cs
│   │   ├── UserProfileMapper.cs
│   │   └── UserSettingsMapper.cs
│   ├── Orders/
│   │   ├── OrderMapper.cs
│   │   ├── OrderItemMapper.cs
│   │   └── InvoiceMapper.cs
│   └── Products/
│       ├── ProductMapper.cs
│       └── CategoryMapper.cs
```

### Keep Mapping Logic Close to DTOs

Consider keeping mappers in the same namespace or near the DTOs they work with:

```csharp
namespace YourApp.Models.DTOs
{
    public class UserDto { /* ... */ }
    
    [Mapper]
    public partial class UserMapper
    {
        public partial UserDto ToDto(User source);
    }
}
```

### Use Partial Classes for Organization

For very complex mappers, split configuration across multiple files:

```csharp
// UserMapper.cs
[Mapper]
public partial class UserMapper
{
    public partial UserDto ToDto(User source);
}

// UserMapper.Configuration.cs
public partial class UserMapper
{
    public UserMapper()
    {
        MapMember(dto => dto.FullName, user => $"{user.FirstName} {user.LastName}");
        // ... more configuration
    }
}

// UserMapper.Helpers.cs
public partial class UserMapper
{
    private static string FormatPhone(string phone) { /* ... */ }
    private static string FormatAddress(Address address) { /* ... */ }
}
```

## Summary

Following these best practices will help you:
- Write maintainable and testable mapping code
- Achieve optimal performance
- Reduce bugs and issues
- Make your codebase easier to understand

## Next Steps

- Learn about [Advanced Usage](../advanced-usage.md)
- See [Core Features](../core-features.md)
- Review [Migration Guides](./from-automapper.md)

