# Advanced Usage

This guide covers advanced Mapgen features and patterns for complex mapping scenarios.

## Table of Contents
- [Constructor Mapping](#constructor-mapping)
- [Mapper Constructor Validation](#mapper-constructor-validation)
- [Mapper Composition](#mapper-composition)
- [Complex Collection Scenarios](#complex-collection-scenarios)
- [Multi-Parameter Mapping](#multi-parameter-mapping)
- [Conditional Mapping](#conditional-mapping)
- [Flattening and Unflattening](#flattening-and-unflattening)
- [Type Conversions](#type-conversions)
- [Testing Mappers](#testing-mappers)
- [Performance Optimization](#performance-optimization)

## Constructor Mapping

When mapping to destination types with readonly properties or types that require constructor parameters, Mapgen provides constructor mapping support.

### Why Constructor Mapping?

Constructor mapping is essential for:
- **Readonly properties** - Properties with only getters that must be set via constructor
- **Immutable objects** - Classes and records that enforce immutability
- **Required constructor parameters** - Types with no parameterless constructor
- **Domain-driven design** - Entities that require valid state from creation

### Automatic Constructor Mapping

**Mapgen automatically detects and uses constructors when possible**, eliminating the need for explicit `UseConstructor()` calls in simple scenarios.

#### When Auto-Mapping Works

Automatic constructor mapping is applied when:
1. The destination type has **exactly 1 constructor** (parameterless or parameterized)
2. All constructor parameters match source properties **by name** (case-insensitive)
3. All parameter types are **compatible** (same type or implicit conversion exists, e.g., `int` → `long`)

```csharp
// Source
public class Product
{
  public string Name { get; set; }
  public decimal Price { get; set; }
  public string Category { get; set; }
}

// Destination with single parameterized constructor
public class ProductDto
{
  public string Name { get; }
  public decimal Price { get; }
  public required string Category { get; init; }

  public ProductDto(string name, decimal price)
  {
    Name = name;
    Price = price;
  }
}

// Mapper - NO UseConstructor() needed!
[Mapper]
public partial class ProductMapper
{
  public partial ProductDto ToDto(Product source);
  
  // Constructor automatically detected and used ✅
}
```

**Generated code:**
```csharp
return new ProductDto(
  source.Name,    // Auto-mapped to constructor
  source.Price    // Auto-mapped to constructor
) {
  Category = source.Category  // Remaining properties via initializer
};
```

#### When Explicit Configuration Is Required

You **must** use `UseConstructor()` or `UseEmptyConstructor()` when:

**1. Multiple Constructors Exist (Ambiguity)**

```csharp
public class OrderDto
{
  public OrderDto() { }  // Constructor 1
  public OrderDto(int id, string status) { }  // Constructor 2
}

// ❌ ERROR: Ambiguous - must specify which constructor
[Mapper]
public partial class OrderMapper
{
  public partial OrderDto ToDto(Order source);
  
  public OrderMapper()
  {
    // Must choose one:
    UseEmptyConstructor();  // Use parameterless
    // OR
    UseConstructor(
      source => source.Id,
      source => source.Status
    );  // Use parameterized
  }
}
```

**2. Constructor Parameters Don't Match Source**

```csharp
public class ProductDto
{
  public ProductDto(string productName, decimal price) { }
  // Parameter name "productName" doesn't match source property "Name"
}

// ❌ ERROR: Can't auto-map - must use UseConstructor()
```

**3. Custom Transformations Needed**

```csharp
public ProductMapper()
{
  UseConstructor(
    source => source.Name.ToUpper(),  // Transformation
    source => source.Price * 1.1m     // Calculation
  );
}
```

> **Important Rule:** Mapgen will show an **error** (MAPPER008) if multiple constructors exist without explicit configuration. This prevents ambiguity and ensures predictable behavior.

#### Automatic Enum Mapping in Constructors

Just like property mapping, **Mapgen automatically maps enum types by name in constructor parameters**. When a constructor parameter is an enum that differs from the source property enum type, Mapgen generates static helper methods that can be used both automatically and manually in your configurations.

**How it works:**

1. **Automatic mapping** - When constructor auto-mapping is possible, enum parameters are mapped automatically
2. **Helper methods generated** - Mapgen generates `MapTo{EnumName}` helper methods for enum conversions
3. **Available for manual use** - These helpers are available in `UseConstructor()` configurations

**Example - Automatic enum mapping in constructor:**

```csharp
// Source enums
public enum OrderPriority { Low, Medium, High }
public enum OrderStatus { Pending, Shipped, Delivered, Cancelled }

// Destination enums (different types)
public enum OrderPriorityDto { Low, Medium, High }
public enum OrderStatusDto { Pending, Shipped, Delivered, Cancelled, Reviewed }

// Source
public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public OrderPriority OrderPriority { get; set; }
    public OrderStatus? CurrentStatus { get; set; }
    public List<OrderStatus> StatusHistory { get; set; }
}

// Destination with enum constructor parameter
public class OrderDto
{
    public int Id { get; }
    public int CustomerId { get; }
    public bool IsVipOrder { get; }
    public OrderStatusDto? CurrentStatus { get; set; }
    public IImmutableList<OrderStatusDto> StatusHistory { get; set; }

    // Constructor with enum parameter - automatically mapped!
    public OrderDto(int id, int customerId, OrderPriorityDto orderPriority)
    {
        Id = id;
        CustomerId = customerId;
        IsVipOrder = orderPriority == OrderPriorityDto.High;
    }
}

// Mapper - NO configuration needed, enums automatically mapped!
[Mapper]
public partial class OrderMapper
{
    public partial OrderDto ToDto(Order order);
    // ✅ OrderPriority → OrderPriorityDto (constructor param)
    // ✅ CurrentStatus → OrderStatusDto? (nullable property)
    // ✅ StatusHistory collection elements mapped
}
```

**Generated code:**
```csharp
public OrderDto ToDto(Order order)
{
    return new OrderDto(
        order.Id,
        order.CustomerId,
        MapToOrderPriorityDto(order.OrderPriority)  // Helper method auto-generated
    )
    {
        CurrentStatus = MapToOrderStatusDto(order.CurrentStatus),
        StatusHistory = order.StatusHistory
            .Select(orderStatus => MapToOrderStatusDto(orderStatus))
            .ToImmutableList()
    };
}

// Helper methods generated automatically:
private static OrderPriorityDto MapToOrderPriorityDto(OrderPriority value) 
    => value switch
    {
        OrderPriority.Low => OrderPriorityDto.Low,
        OrderPriority.Medium => OrderPriorityDto.Medium,
        OrderPriority.High => OrderPriorityDto.High,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unexpected enum value")
    };

private static OrderStatusDto MapToOrderStatusDto(OrderStatus value) 
    => value switch
    {
        OrderStatus.Pending => OrderStatusDto.Pending,
        OrderStatus.Shipped => OrderStatusDto.Shipped,
        OrderStatus.Delivered => OrderStatusDto.Delivered,
        OrderStatus.Cancelled => OrderStatusDto.Cancelled,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unexpected enum value")
    };

// Nullable overloads also generated:
private static OrderPriorityDto? MapToOrderPriorityDto(OrderPriority? value) 
    => value.HasValue ? MapToOrderPriorityDto(value.Value) : null;

private static OrderStatusDto? MapToOrderStatusDto(OrderStatus? value) 
    => value.HasValue ? MapToOrderStatusDto(value.Value) : null;
```

**Using helper methods in manual configurations:**

Even when you need to use explicit `UseConstructor()`, **the enum helper methods are automatically generated and available for you to use**:

```csharp
public class OrderDto
{
    public int Id { get; }
    public int CustomerId { get; }
    public string DisplayStatus { get; }
    
    public OrderDto(int id, int customerId, OrderStatusDto status)
    {
        Id = id;
        CustomerId = customerId;
        DisplayStatus = status.ToString();
    }
    
    // Multiple constructors = must use UseConstructor()
    public OrderDto() { }
}

[Mapper]
public partial class OrderMapper
{
    public partial OrderDto ToDto(Order order);
    
    public OrderMapper()
    {
        // Even with explicit UseConstructor(), helper methods are generated!
        UseConstructor(
            src => src.Id,
            src => src.CustomerId,
            src => MapToOrderStatusDto(src.Status)  // ✅ Helper available!
        );
    }
}
```

**Key benefits:**
- ✅ **Code reuse** - Same helper methods for constructor params, properties, and collections
- ✅ **Name-based mapping** - Enums mapped by member name, not numeric value
- ✅ **Nullable support** - Both nullable and non-nullable overloads generated automatically
- ✅ **Collection support** - Enum collections in constructor parameters work seamlessly
- ✅ **Available in config** - Helper methods can be used in `UseConstructor()` expressions

**When helper methods are generated:**
- Mapgen analyzes constructor parameters and source properties
- When it finds enum type mismatches (different enum types with matching names), it generates helpers
- Works for automatic constructor mapping AND explicit `UseConstructor()` configurations
- You can reference these helpers by name: `MapTo{DestinationEnumName}`

**Requirements:**
- Constructor parameter must be an enum (or nullable enum, or collection of enums)
- Source property with matching name must exist and be an enum of a different type
- All source enum members must exist in destination enum (destination can have extra members)
- Member names must match exactly (case-sensitive)

**If requirements aren't met:**
- If source has members not in destination → MAPPER012 error (see [MAPPER012 diagnostic](#mapper012-incompatible-enum-mapping) for details and solutions)

### Explicit Constructor Mapping

When automatic constructor detection isn't possible or you need custom transformations, use `UseConstructor()` to explicitly specify constructor parameters:

```csharp
// Destination with readonly properties
public class ProductDto
{
  public string Name { get; }           // Readonly - via constructor
  public decimal Price { get; }         // Readonly - via constructor
  public required string Category { get; init; }  // Required - via initializer

  public ProductDto(string name, decimal price)
  {
    Name = name;
    Price = price;
  }
}

[Mapper]
public partial class ProductMapper
{
  public partial ProductDto ToDto(Product source);

  public ProductMapper()
  {
    // Explicit constructor configuration
    UseConstructor(
      source => source.Name,
      source => source.Price
    );
    // Category is mapped via object initializer automatically
  }
}
```

**Generated code:**
```csharp
return new ProductDto(
  source.Name,
  source.Price
) {
  Category = source.Category  // Via object initializer
};
```

> **Important:** Constructor parameter names must match the property names (case-insensitive) for Mapgen to recognize which properties are being set. In the example above, the constructor parameter `name` matches the property `Name`, and `price` matches `Price`. This allows Mapgen to know that these properties are handled by the constructor and should not be set via the object initializer.

### Constructor with Multiple Parameters

Map multiple source properties to constructor parameters:

```csharp
public class AddressDto
{
  public string Street { get; }
  public string City { get; }
  public string ZipCode { get; }

  public AddressDto(string street, string city, string zipCode)
  {
    Street = street;
    City = city;
    ZipCode = zipCode;
  }
}

[Mapper]
public partial class AddressMapper
{
  public partial AddressDto ToDto(Address source);

  public AddressMapper()
  {
    UseConstructor(
      source => source.Street,
      source => source.City,
      source => source.ZipCode
    );
  }
}
```

### Constructor with Transformations

Apply transformations to constructor parameters:

```csharp
[Mapper]
public partial class PersonMapper
{
  public partial PersonDto ToDto(Person source);

  public PersonMapper()
  {
    UseConstructor(
      source => source.FirstName.Trim(),
      source => source.LastName.ToUpper(),
      source => source.Age > 0 ? source.Age : 0
    );
  }
}
```

### Constructor with Multiple Source Parameters

Use multiple source parameters in constructor mappings:

```csharp
[Mapper]
public partial class OrderMapper
{
  public partial OrderDto ToDto(Order order, Customer customer);

  public OrderMapper()
  {
    UseConstructor(
      (order, customer) => order.Id,
      (order, customer) => customer.Name,
      (order, customer) => order.Total
    );
  }
}
```

### Using Empty Constructor Explicitly

When a type has multiple constructors, explicitly choose the parameterless one:

```csharp
public class CustomerDto
{
  public string Name { get; set; }
  public string Email { get; set; }

  public CustomerDto() { }  // Parameterless
  public CustomerDto(string name, string email) { /* ... */ }  // Parameterized
}

[Mapper]
public partial class CustomerMapper
{
  public partial CustomerDto ToDto(Customer source);

  public CustomerMapper()
  {
    UseEmptyConstructor();  // Explicitly use parameterless constructor
  }
}
```

### Constructor Mapping with Records

Records work seamlessly with constructor mapping:

```csharp
// Record with primary constructor
public record PersonDto(string FirstName, string LastName, int Age)
{
  public string Email { get; init; } = string.Empty;
}

[Mapper]
public partial class PersonMapper
{
  public partial PersonDto ToDto(Person source);

  public PersonMapper()
  {
    UseConstructor(
      source => source.FirstName,
      source => source.LastName,
      source => source.Age
    );
    // Email is mapped via object initializer
  }
}
```

**Generated code:**
```csharp
return new PersonDto(
  source.FirstName,
  source.LastName,
  source.Age
) {
  Email = source.Email
};
```

### Constructor Mapping with Inheritance

Map derived types that call base constructors:

```csharp
public class VehicleDto
{
  public string Make { get; }
  public string Model { get; }

  public VehicleDto(string make, string model)
  {
    Make = make;
    Model = model;
  }
}

public class CarDto : VehicleDto
{
  public int NumberOfDoors { get; }

  public CarDto(string make, string model, int doors) 
    : base(make, model)  // Calls base constructor
  {
    NumberOfDoors = doors;
  }
}

[Mapper]
public partial class CarMapper
{
  public partial CarDto ToDto(Car source);

  public CarMapper()
  {
    UseConstructor(
      source => source.Make,         // Base property
      source => source.Model,        // Base property
      source => source.NumberOfDoors // Derived property
    );
  }
}
```

### Constructor Parameter Names

Mapgen uses the actual method parameter names in generated code:

```csharp
[Mapper]
public partial class ProductMapper
{
  public partial ProductDto ToDto(Product source);

  public ProductMapper()
  {
    // You can use any parameter names you prefer
    UseConstructor(
      prod => prod.Name,        // ✅ "prod" will be replaced with "source"
      p => p.Price              // ✅ "p" will be replaced with "source"
    );
  }
}
```

#### Parameter Name Matching

**Critical:** Constructor parameter names must match the destination property names (case-insensitive) for Mapgen to correctly identify which properties are set by the constructor.

```csharp
public class AddressDto
{
  public string Street { get; }
  public string City { get; }
  public string ZipCode { get; }

  // ✅ Good - parameter names match property names
  public AddressDto(string street, string city, string zipCode)
  {
    Street = street;
    City = city;
    ZipCode = zipCode;
  }
}

[Mapper]
public partial class AddressMapper
{
  public AddressMapper()
  {
    UseConstructor(
      source => source.Street,
      source => source.City,
      source => source.ZipCode
    );
  }
}
```

**What happens:** Mapgen matches constructor parameters to properties by name:
- Constructor parameter `street` → Property `Street` ✅
- Constructor parameter `city` → Property `City` ✅
- Constructor parameter `zipCode` → Property `ZipCode` ✅

#### Naming Mismatch Example

```csharp
public class ProductDto
{
  public string Name { get; }
  public decimal Price { get; }

  // ❌ Parameter names don't match property names
  public ProductDto(string productName, decimal productPrice)
  {
    Name = productName;
    Price = productPrice;
  }
}

[Mapper]
public partial class ProductMapper
{
  public ProductMapper()
  {
    UseConstructor(
      source => source.Name,
      source => source.Price
    );
  }
}
```

**Problem:** Mapgen cannot determine that:
- Constructor parameter `productName` sets property `Name`
- Constructor parameter `productPrice` sets property `Price`

**Result:** Mapgen may try to set `Name` and `Price` via object initializer (which will fail for readonly properties) or report unmapped properties.

#### Best Practice: Use Standard Naming

Follow C# conventions where constructor parameter names are camelCase versions of property names:

```csharp
// ✅ Standard C# convention
public class PersonDto
{
  public string FirstName { get; }
  public string LastName { get; }
  public int Age { get; }

  public PersonDto(string firstName, string lastName, int age)
  //                      ↓               ↓            ↓
  //               Matches FirstName  LastName      Age
  {
    FirstName = firstName;
    LastName = lastName;
    Age = age;
  }
}
```

#### Records Automatically Follow This Pattern

Records with primary constructors automatically follow the correct naming convention:

```csharp
// ✅ Parameter names automatically match property names
public record PersonDto(string FirstName, string LastName, int Age);
//                             ↓               ↓            ↓
//                      Creates properties: FirstName, LastName, Age
//                      With parameters:    firstName, lastName, age
```

## Constructor Mapping Diagnostics

Mapgen provides comprehensive diagnostics to help you use constructor mapping correctly.

### MAPPER007: Parameterized Constructor Required

**When it occurs:** Destination type has a single parameterized constructor, but it cannot be automatically mapped (parameters don't match source properties or types are incompatible).

```csharp
public class ProductDto
{
  // Constructor parameters don't match source property names
  public ProductDto(string productName, decimal productPrice) { /* ... */ }
}

[Mapper]
public partial class ProductMapper
{
  public partial ProductDto ToDto(Product source);  // Source has Name and Price
  
  // ❌ ERROR MAPPER007: Can't auto-map - parameter names don't match
}
```

**Fix:**
```csharp
public ProductMapper()
{
  UseConstructor(
    source => source.Name,    // Maps to productName
    source => source.Price    // Maps to productPrice
  );
}
```

**Note:** If the constructor parameters matched source properties exactly (e.g., `name`, `price`), it would auto-map without needing `UseConstructor()`.

### MAPPER008: Ambiguous Constructor Selection

**When it occurs:** Destination type has both parameterless and parameterized constructors, and neither `UseConstructor()` nor `UseEmptyConstructor()` is specified.

```csharp
public class CustomerDto
{
  public CustomerDto() { }
  public CustomerDto(string name, string email) { }
  // Multiple constructors - ambiguous!
}

[Mapper]
public partial class CustomerMapper
{
  public partial CustomerDto ToDto(Customer source);
  
  // ❌ ERROR MAPPER008: Ambiguous constructor selection
}
```

**Fix - Option 1: Use Empty Constructor**
```csharp
public CustomerMapper()
{
  UseEmptyConstructor();  // Explicitly use parameterless
}
```

**Fix - Option 2: Use Parameterized Constructor**
```csharp
public CustomerMapper()
{
  UseConstructor(
    source => source.Name,
    source => source.Email
  );
}
```

**Important:** This error occurs whenever 2 or more constructors exist, regardless of whether any are auto-mappable. Explicit selection is required to avoid ambiguity.

**Examples of ambiguous scenarios:**
- Parameterless + Parameterized constructor(s)
- Multiple parameterized constructors
- Any combination with 2+ total constructors


### MAPPER010: UseEmptyConstructor Not Possible

**When it occurs:** `UseEmptyConstructor()` is called but destination type has no parameterless constructor.

```csharp
public class ProductDto
{
  public ProductDto(string name, decimal price) { }
  // No parameterless constructor
}

[Mapper]
public partial class ProductMapper
{
  public partial ProductDto ToDto(Product source);
  
  public ProductMapper()
  {
    UseEmptyConstructor();  // ❌ ERROR MAPPER010: No parameterless constructor
  }
}
```

**Fix:**
```csharp
public ProductMapper()
{
  UseConstructor(
    source => source.Name,
    source => source.Price
  );
}
```

### MAPPER012: Incompatible Enum Mapping

**When it occurs:** Source enum has members that don't exist in the destination enum. This prevents automatic enum mapping because Mapgen cannot safely map values that don't have a corresponding destination member.

**Applies to:**
- Property mappings (direct enum properties)
- Constructor parameter mappings (enum constructor arguments)
- Collection mappings (collections of enums)

```csharp
// Source enum has "Cancelled" but destination doesn't
public enum PaymentStatus { Pending, Approved, Rejected, Cancelled }
public enum PaymentStatusDto { Pending, Approved, Rejected }

public class Payment
{
    public PaymentStatus Status { get; set; }
}

public class PaymentDto
{
    public PaymentStatusDto Status { get; set; }
}

[Mapper]
public partial class PaymentMapper
{
    public partial PaymentDto ToDto(Payment payment);
    
    // ❌ ERROR MAPPER012: Source enum has member "Cancelled" not present in destination
}
```

**Error message example:**
```
error MAPPER012: Enum property "PaymentDto.Status" of type "PaymentStatusDto" 
cannot be automatically mapped from "Payment.Status" of type "PaymentStatus" 
because source enum has members not present in destination: "Cancelled". 
Use MapMember() to create custom mapping with explicit handling for these values.
```

**Fix - Use MapMember() for custom enum mapping:**
```csharp
public PaymentMapper()
{
    MapMember(dest => dest.Status, src => src.Status switch
    {
        PaymentStatus.Pending => PaymentStatusDto.Pending,
        PaymentStatus.Approved => PaymentStatusDto.Approved,
        PaymentStatus.Rejected => PaymentStatusDto.Rejected,
        PaymentStatus.Cancelled => PaymentStatusDto.Rejected,  // Explicit handling
        _ => throw new ArgumentOutOfRangeException()
    });
}
```

**MAPPER012 in constructor parameters:**

When a constructor parameter is an enum with missing members, the same error applies:

```csharp
public enum OrderPriority { Low, Medium, High, Critical }  // Source has "Critical"
public enum OrderPriorityDto { Low, Medium, High }         // Destination doesn't

public class Order
{
    public int Id { get; set; }
    public OrderPriority Priority { get; set; }
}

public class OrderDto
{
    public int Id { get; }
    
    // Constructor with enum parameter
    public OrderDto(int id, OrderPriorityDto priority)
    {
        Id = id;
        // ... use priority
    }
}

[Mapper]
public partial class OrderMapper
{
    public partial OrderDto ToDto(Order order);
    
    // ❌ ERROR MAPPER012: Constructor parameter "priority" has incompatible enum
}
```

**Fix - Use UseConstructor() with custom enum mapping:**
```csharp
public OrderMapper()
{
    UseConstructor(
        src => src.Id,
        src => src.Priority switch
        {
            OrderPriority.Low => OrderPriorityDto.Low,
            OrderPriority.Medium => OrderPriorityDto.Medium,
            OrderPriority.High => OrderPriorityDto.High,
            OrderPriority.Critical => OrderPriorityDto.High,  // Map to High
            _ => throw new ArgumentOutOfRangeException()
        }
    );
}
```

**Why this error is important:**
- ✅ **Type safety** - Prevents runtime errors from unmapped enum values
- ✅ **Explicit intent** - Forces you to decide how to handle missing members
- ✅ **Documentation** - Your switch expression serves as documentation for edge cases
- ✅ **Compile-time safety** - Catches the issue during build, not at runtime

**Common patterns for handling missing members:**
1. **Map to closest equivalent** - `Cancelled → Rejected`
2. **Map to default value** - `Unknown → Pending`
3. **Throw exception** - For truly invalid states
4. **Use nullable destination** - Map to `null` if appropriate

**Note:** Destination enums can have *extra* members (not present in source). Only missing destination members cause MAPPER012.

### Understanding Diagnostic Messages

All constructor diagnostics provide:
- ✅ Clear error message explaining the problem
- ✅ List of available constructors with their signatures
- ✅ Suggested fix (use `UseConstructor()` or `UseEmptyConstructor()`)
- ✅ Exact location in code where the issue occurs

**Example diagnostic output:**
```
error MAPPER007: Cannot generate mapping to 'ProductDto'. Type has a constructor 
with parameters but no parameterless constructor. Use 'UseConstructor()' to 
specify constructor parameters.

Available constructors:
  - ProductDto(string name, decimal price)
```

### Summary of Constructor Mapping Rules

**Automatic Mapping (No configuration needed):**
- ✅ Exactly 1 constructor exists
- ✅ All constructor parameters match source properties by name (case-insensitive)
- ✅ All parameter types are compatible (same or implicit conversion)

**Explicit Configuration Required:**
- ❌ 2 or more constructors exist → MAPPER008 error (must use `UseConstructor()` or `UseEmptyConstructor()`)
- ❌ Single constructor but parameters don't match source → MAPPER007 error (must use `UseConstructor()`)
- ❌ Enum parameter with incompatible members → MAPPER012 error (must use custom switch expression)
- ❌ Custom transformations or calculations needed → Use `UseConstructor()`

**Key Principle:** Mapgen prioritizes clarity and prevents ambiguity. When in doubt, it requires explicit configuration rather than making assumptions.

## Constructor Mapping Best Practices

### 1. Leverage Auto-Mapping When Possible

Design your DTOs to enable automatic constructor mapping:

```csharp
// ✅ Excellent - auto-maps without configuration
public class ProductDto
{
  public string Name { get; }
  public decimal Price { get; }
  public required string Category { get; init; }

  // Single constructor with parameters matching source property names
  public ProductDto(string name, decimal price)
  {
    Name = name;
    Price = price;
  }
}

[Mapper]
public partial class ProductMapper
{
  public partial ProductDto ToDto(Product source);
  // No constructor needed - auto-detected! ✅
}
```

**Tips for auto-mapping:**
- Use single constructor when possible
- Name constructor parameters to match source property names (case-insensitive)
- Use standard C# naming: camelCase parameters, PascalCase properties

### 2. Prefer Readonly Properties for Immutability

```csharp
// ✅ Good - immutable via constructor
public class ProductDto
{
  public string Name { get; }
  public decimal Price { get; }

  public ProductDto(string name, decimal price)
  {
    Name = name;
    Price = price;
  }
}
```

### 3. Combine Constructor + Object Initializer

```csharp
public class OrderDto
{
  public int Id { get; }              // Via constructor
  public decimal Total { get; }       // Via constructor
  public required string Status { get; init; }  // Via initializer

  public OrderDto(int id, decimal total) { /* ... */ }
}

[Mapper]
public partial class OrderMapper
{
  public OrderMapper()
  {
    UseConstructor(
      source => source.Id,
      source => source.Total
    );
    // Status mapped automatically via initializer
  }
}
```

### 4. Use Records for DTOs

Records provide built-in immutability and concise syntax:

```csharp
// ✅ Excellent for DTOs - auto-maps perfectly
public record ProductDto(string Name, decimal Price)
{
  public required string Category { get; init; }
}
```

### 5. Avoid Multiple Constructors When Possible

Multiple constructors require explicit configuration:

```csharp
// ❌ Requires explicit UseConstructor() or UseEmptyConstructor()
public class CustomerDto
{
  public CustomerDto() { }
  public CustomerDto(string name) { }
}

// ✅ Better - single constructor enables auto-mapping
public class CustomerDto
{
  public string Name { get; }
  
  public CustomerDto(string name)
  {
    Name = name;
  }
}
```

### 6. Handle Constructor Validation

If constructors perform validation, ensure mapped values are valid:

```csharp
[Mapper]
public partial class PersonMapper
{
  public PersonMapper()
  {
    UseConstructor(
      source => source.Name ?? "Unknown",  // Prevent null
      source => Math.Max(0, source.Age)    // Ensure valid age
    );
  }
}
```

### 7. Order Constructor Parameters Logically

Match the order of constructor parameters for clarity:

```csharp
// Constructor signature: (string street, string city, string zipCode)
UseConstructor(
  source => source.Street,   // 1st parameter
  source => source.City,     // 2nd parameter
  source => source.ZipCode   // 3rd parameter
);
```

## Common Constructor Mapping Scenarios

### Scenario 1: Auto-Mapping with Single Constructor (Recommended)

**Solution:** Design DTOs with single constructor that matches source properties

```csharp
// Source
public class Product
{
  public string Name { get; set; }
  public decimal Price { get; set; }
}

// Destination
public class ProductDto
{
  public string Name { get; }
  public decimal Price { get; }
  
  public ProductDto(string name, decimal price)
  {
    Name = name;
    Price = price;
  }
}

[Mapper]
public partial class ProductMapper
{
  public partial ProductDto ToDto(Product source);
  // Auto-maps! No configuration needed ✅
}
```

### Scenario 2: Readonly Properties with Explicit Mapping

**Problem:** Properties can only be set in constructor, but need transformations
```csharp
public class ImmutableProduct
{
  public string Name { get; }
  public decimal Price { get; }
  
  public ImmutableProduct(string name, decimal price) { /* ... */ }
}
```

**Solution:**
```csharp
[Mapper]
public partial class ProductMapper
{
  public ProductMapper()
  {
    UseConstructor(
      source => source.Name.ToUpper(),    // Transformation
      source => source.Price * 1.1m       // Calculation
    );
  }
}
```

### Scenario 3: Required Properties

**Problem:** Some properties are required and must be set
```csharp
public class OrderDto
{
  public int Id { get; }
  public required string Status { get; init; }
  
  public OrderDto(int id) { /* ... */ }
}
```

**Solution:**
```csharp
[Mapper]
public partial class OrderMapper
{
  public OrderMapper()
  {
    UseConstructor(source => source.Id);
    // Status mapped automatically via initializer (required property)
  }
}
```

### Scenario 4: Value Objects

**Problem:** Domain value objects with validation in constructor
```csharp
public class Money
{
  public decimal Amount { get; }
  public string Currency { get; }
  
  public Money(decimal amount, string currency)
  {
    if (amount < 0) throw new ArgumentException("Amount cannot be negative");
    Amount = amount;
    Currency = currency ?? throw new ArgumentNullException(nameof(currency));
  }
}
```

**Solution:**
```csharp
[Mapper]
public partial class MoneyMapper
{
  public MoneyMapper()
  {
    UseConstructor(
      source => Math.Max(0, source.Amount),  // Ensure valid
      source => source.Currency ?? "USD"     // Prevent null
    );
  }
}
```

### Scenario 5: Inheritance Hierarchies

**Problem:** Derived class calls base constructor
```csharp
public class VehicleDto
{
  public VehicleDto(string make, string model) { /* ... */ }
}

public class CarDto : VehicleDto
{
  public CarDto(string make, string model, int doors) : base(make, model) { /* ... */ }
}
```

**Solution:**
```csharp
[Mapper]
public partial class CarMapper
{
  public CarMapper()
  {
    UseConstructor(
      source => source.Make,
      source => source.Model,
      source => source.NumberOfDoors
    );
  }
}
```

## Mapper Constructor Validation

Mapgen enforces strict validation rules for mapper constructors to ensure clean, maintainable mapper definitions and prevent common configuration mistakes.

### Constructor Rules

**Mapper constructors must:**
- ✅ Only contain mapping configuration method calls
- ✅ Not have any parameters
- ✅ Not contain arbitrary expressions or statements

**Allowed configuration methods:**
- `MapMember()` - Custom property mapping
- `MapCollection()` - Custom collection mapping
- `IgnoreMember()` - Ignore specific properties
- `UseConstructor()` - Configure constructor parameters
- `UseEmptyConstructor()` - Use parameterless constructor
- `IncludeMappers()` - Include other mappers

### MAPPER010: Constructor Parameters Not Allowed

**When it occurs:** Mapper constructor contains parameters.

```csharp
// ❌ ERROR: Mapper constructor cannot have parameters
[Mapper]
public partial class PersonMapper
{
    public partial PersonDto ToDto(Person source);

    // ❌ MAPPER010: Constructor cannot have parameters
    public PersonMapper(ILogger logger)
    {
        MapMember(dto => dto.FullName, 
                  person => $"{person.FirstName} {person.LastName}");
    }
}
```

**Why this rule exists:**
- Mappers are designed to be stateless and reusable
- Parameters would complicate the auto-generated extension methods
- Dependencies should be handled through dependency injection at a higher level, not in mappers

**Fix:**
```csharp
// ✅ Correct: No parameters
[Mapper]
public partial class PersonMapper
{
    public partial PersonDto ToDto(Person source);

    public PersonMapper()
    {
        MapMember(dto => dto.FullName, 
                  person => $"{person.FirstName} {person.LastName}");
    }
}
```

**Alternative approach for dependencies:**
```csharp
// If you need to inject dependencies, use a wrapper class
public class PersonMappingService
{
    private readonly ILogger _logger;
    private readonly PersonMapper _mapper;

    public PersonMappingService(ILogger logger)
    {
        _logger = logger;
        _mapper = new PersonMapper();
    }

    public PersonDto MapToDto(Person person)
    {
        _logger.LogInformation("Mapping person {Id}", person.Id);
        return _mapper.ToDto(person);
    }
}
```

### MAPPER011: Invalid Constructor statement

**When it occurs:** Mapper constructor contains expressions or statements other than mapping configuration method calls.

```csharp
// ❌ ERROR: Constructor contains non-configuration expressions
[Mapper]
public partial class ProductMapper
{
    public partial ProductDto ToDto(Product source);

    public ProductMapper()
    {
        // ❌ MAPPER011: Variable declarations not allowed
        var defaultCategory = "Uncategorized";
        
        // ❌ MAPPER011: Arbitrary expressions not allowed
        Console.WriteLine("Initializing mapper");
        
        // ✅ This is allowed - it's a configuration method
        MapMember(dto => dto.Category, 
                  product => product.Category ?? "Uncategorized");
    }
}
```

**Fix:**
```csharp
// ✅ Correct: Only configuration method calls
[Mapper]
public partial class ProductMapper
{
    public partial ProductDto ToDto(Product source);

    public ProductMapper()
    {
        // All configuration inline - no variables or arbitrary code
        MapMember(dto => dto.Category, 
                  product => product.Category ?? "Uncategorized");
        
        IgnoreMember(dto => dto.InternalId);
        
        UseEmptyConstructor();
    }
}
```

**Common violations and fixes:**

**❌ Variable declarations:**
```csharp
public ProductMapper()
{
    var defaultValue = "N/A";  // ❌ Not allowed
    MapMember(dto => dto.Name, p => p.Name ?? defaultValue);
}
```

**✅ Use inline values:**
```csharp
public ProductMapper()
{
    MapMember(dto => dto.Name, p => p.Name ?? "N/A");  // ✅ Correct
}
```

**❌ Conditional statements:**
```csharp
public ProductMapper()
{
    if (someCondition)  // ❌ Not allowed
    {
        MapMember(dto => dto.Name, p => p.Name);
    }
}
```

**✅ Use configuration methods directly:**
```csharp
public ProductMapper()
{
    MapMember(dto => dto.Name, p => p.Name);  // ✅ Always configure
}
```

**❌ Method calls to non-configuration methods:**
```csharp
public ProductMapper()
{
    InitializeDefaults();  // ❌ Not allowed
    MapMember(dto => dto.Name, p => p.Name);
}
```

**✅ Only use configuration methods:**
```csharp
public ProductMapper()
{
    MapMember(dto => dto.Name, p => p.Name);  // ✅ Correct
    IgnoreMember(dto => dto.InternalId);      // ✅ Correct
}
```

All three approaches are equivalent and generate the same code. Use whichever style you prefer.

### Why These Rules Matter

**Before (v1.0.1):** Invalid constructor content was silently ignored, leading to confusion:
```csharp
public ProductMapper(ILogger logger)  // Silently ignored - no error!
{
    var defaultValue = "N/A";  // Ignored
    // Generated code cannot access defaultValue.
    MapMember(dto => dto.Name, p => p.Name ?? defaultName);
}
```

**After (v1.0.1):** Clear diagnostics help you identify issues immediately:
```csharp
public ProductMapper(ILogger logger)  // ❌ MAPPER010: Clear error!
{
    var defaultValue = "N/A";  // ❌ MAPPER011: Clear error!
    MapMember(dto => dto.Name, p => p.Name ?? "N/A");  // ✅ Works
}
```

**Benefits:**
- 🎯 **Clear feedback** - Know immediately when something is wrong
- 🧹 **Cleaner code** - Enforces best practices
- 📖 **Better maintainability** - Mappers are easier to understand
- 🐛 **Fewer bugs** - Catch configuration errors at compile time

### Summary

| Rule | Error Code | Description |
|------|------------|-------------|
| No parameters | MAPPER010  | Constructor cannot have parameters |
| Configuration only | MAPPER011  | Only mapping configuration methods allowed |

**Remember:** These validations only affect mapper constructors, not the mapping methods themselves. Your mapping logic can be as complex as needed using `MapMember()` expressions.

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
