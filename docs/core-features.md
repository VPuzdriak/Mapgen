# Core Features

## Table of Contents
- [Automatic Property Mapping](#automatic-property-mapping)
- [Field Mapping](#field-mapping)
- [Custom Property Mapping](#custom-property-mapping)
- [Multiple Parameters](#multiple-parameters)
- [Collection Mapping](#collection-mapping)
- [Nested Object Mapping](#nested-object-mapping)
- [Ignoring Properties](#ignoring-properties)
- [Fully Qualified Type Names](#fully-qualified-type-names)
- [Extension Methods](#extension-methods)

## Automatic Property Mapping

Mapgen automatically maps properties when:
- Property names match exactly (case-sensitive)
- Types are the same or implicitly convertible

```csharp
public class Person
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
}

public class PersonDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
}

[Mapper]
public partial class PersonMapper
{
    public partial PersonDto ToDto(Person source);
    // No configuration needed - all properties auto-mapped!
}
```

### Implicit Type Conversions

Mapgen handles implicit conversions automatically:

```csharp
public class Source
{
    public int Number { get; set; }      // int -> long
    public float Value { get; set; }     // float -> double
    public string Text { get; set; }     // string -> string (nullable)
}

public class Destination
{
    public long Number { get; set; }
    public double Value { get; set; }
    public string? Text { get; set; }
}
```

## Field Mapping

Mapgen supports mapping public fields in addition to properties. This is particularly useful when working with DTOs, data structures, or legacy code that uses fields.

### Automatic Field Mapping

Fields are automatically mapped when:
- Field names match exactly (case-sensitive)
- Types are the same or implicitly convertible
- Fields are public and accessible

```csharp
public class Person
{
    public string FirstName;  // Public field
    public string LastName;   // Public field
    public int Age;          // Public field
}

public class PersonDto
{
    public string FirstName { get; set; }  // Property
    public string LastName { get; set; }   // Property
    public int Age { get; set; }          // Property
}

[Mapper]
public partial class PersonMapper
{
    public partial PersonDto ToDto(Person source);
    // Fields automatically mapped to properties!
}
```

### Mixed Fields and Properties

Mapgen seamlessly handles classes with both fields and properties:

```csharp
public class Product
{
    public int Id { get; set; }      // Property
    public string Name;              // Field
    public decimal Price;            // Field
    public string Category { get; set; }  // Property
}

public class ProductDto
{
    public int Id;                   // Field
    public string Name { get; set; } // Property
    public decimal Price { get; set; } // Property
    public string Category;          // Field
}

[Mapper]
public partial class ProductMapper
{
    public partial ProductDto ToDto(Product source);
    // All fields and properties automatically mapped!
}
```

### Field Access Considerations

**Important:** Only public fields are supported for mapping. Private, protected, or internal fields are ignored:

```csharp
public class Account
{
    public string Id;           // ✅ Mapped - public
    private string password;    // ❌ Ignored - private
    protected string token;     // ❌ Ignored - protected
    internal string key;        // ❌ Ignored - internal
}
```

### Readonly Fields

Readonly fields can be mapped through constructor parameters:

```csharp
public class ImmutablePerson
{
    public readonly string Name;  // Readonly field
    public readonly int Age;      // Readonly field

    public ImmutablePerson(string name, int age)
    {
        Name = name;
        Age = age;
    }
}

[Mapper]
public partial class PersonMapper
{
    public partial ImmutablePerson ToImmutable(Person source);

    public PersonMapper()
    {
        UseConstructor(
            source => source.Name,
            source => source.Age
        );
    }
}
```

## Custom Property Mapping

Use `MapMember()` to define custom mapping logic.

### Simple Expression Mapping

```csharp
[Mapper]
public partial class UserMapper
{
    public partial UserDto ToDto(User source);

    public UserMapper()
    {
        // Combine multiple source properties
        MapMember(dto => dto.FullName, 
                  user => $"{user.FirstName} {user.LastName}");
        
        // Apply transformation
        MapMember(dto => dto.Email, 
                  user => user.Email.ToLowerInvariant());
    }
}
```

### Method Reference Mapping

For complex logic, use method references:

```csharp
[Mapper]
public partial class OrderMapper
{
    public partial OrderDto ToDto(Order source);

    public OrderMapper()
    {
        MapMember(dto => dto.Status, GetOrderStatus);
        MapMember(dto => dto.TotalPrice, CalculateTotal);
    }

    private static string GetOrderStatus(Order order)
    {
        if (order.IsPaid && order.IsShipped)
            return "Completed";
        if (order.IsPaid)
            return "Processing";
        return "Pending";
    }

    private static decimal CalculateTotal(Order order)
    {
        return order.Items.Sum(i => i.Price * i.Quantity) + order.ShippingCost;
    }
}
```

### Nested Property Access

Access nested properties in your mapping expressions:

```csharp
public class Car
{
    public string Model { get; set; }
    public CarOwner Owner { get; set; }
}

public class CarOwner
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

public class CarDto
{
    public string Model { get; set; }
    public string OwnerName { get; set; }
}

[Mapper]
public partial class CarMapper
{
    public partial CarDto ToDto(Car source);

    public CarMapper()
    {
        MapMember(dto => dto.OwnerName, 
                  car => $"{car.Owner.FirstName} {car.Owner.LastName}");
    }
}
```

## Multiple Parameters

Mapgen supports mapping methods with multiple parameters:

```csharp
public class CarDto
{
    public string Model { get; set; }
    public string MainDriverName { get; set; }
}

[Mapper]
public partial class CarMapper
{
    // Multiple parameters in the mapping method
    public partial CarDto ToCarDto(Car car, Driver driver);

    public CarMapper()
    {
        // Access additional parameters using positional syntax
        MapMember(dto => dto.MainDriverName, 
                  (car, driver) => $"{driver.FirstName} {driver.LastName}");
        
        // Or use underscore to skip parameters
        MapMember(dto => dto.MainDriverName, 
                  (_, driver) => $"{driver.FirstName} {driver.LastName}");
    }
}

// Usage
var mapper = new CarMapper();
var carDto = mapper.ToCarDto(car, driver);
```

### Complex Multi-Parameter Example

```csharp
[Mapper]
public partial class ShipmentMapper
{
    public partial ShipmentDto ToDto(Shipment shipment, Person sender, Person receiver);

    public ShipmentMapper()
    {
        MapMember(dto => dto.ShipmentId, shipment => shipment.Id);
        MapMember(dto => dto.SenderName, (_, sender, _) => sender.Name);
        MapMember(dto => dto.ReceiverName, (_, _, receiver) => receiver.Name);
        MapMember(dto => dto.SenderAddress, (_, sender, _) => sender.Address);
        MapMember(dto => dto.ReceiverAddress, (_, _, receiver) => receiver.Address);
    }
}
```

## Collection Mapping

### Basic Collection Mapping

Use `MapCollection()` for custom collection transformations:

```csharp
public class Team
{
    public string Name { get; set; }
    public List<Player> Players { get; set; }
}

public class TeamDto
{
    public string Name { get; set; }
    public ImmutableList<PlayerDto> Players { get; set; }
}

[Mapper]
public partial class TeamMapper
{
    public partial TeamDto ToDto(Team source);

    public TeamMapper()
    {
        MapCollection<PlayerDto, Player>(
            dest => dest.Players,
            player => player.ToPlayerDto()
        );
    }
}
```

### Collection Mapping with Multiple Parameters

Pass additional parameters to collection item mappings:

```csharp
public class Garage
{
    public string Address { get; set; }
    public List<Car> Cars { get; set; }
}

public class GarageDto
{
    public string Address { get; set; }
    public ImmutableList<CarDto> Cars { get; set; }
}

[Mapper]
public partial class GarageMapper
{
    public partial GarageDto ToGarageDto(Garage source, Driver driver);

    public GarageMapper()
    {
        // Pass the driver parameter to each car mapping
        MapCollection<CarDto, Car>(
            dest => dest.Cars,
            (car, _, driver) => car.ToCarDto(driver)
        );
    }
}
```

### Collection Mapping with Custom Source

Specify both destination and source collections:

```csharp
public GarageMapper()
{
    MapCollection<CarDto, Car>(
        dest => dest.CarsDto,
        garage => garage.VehicleList,  // Custom source property
        (car, garage, driver) => car.ToCarDto(driver)
    );
}
```

## Nested Object Mapping

Map nested objects by calling other mappers:

```csharp
public class Employee
{
    public int Id { get; set; }
    public string Name { get; set; }
    public Department Department { get; set; }
}

public class EmployeeDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DepartmentDto Department { get; set; }
}

[Mapper]
public partial class EmployeeMapper
{
    public partial EmployeeDto ToDto(Employee source);

    public EmployeeMapper()
    {
        MapMember(dto => dto.Department, 
                  emp => emp.Department.ToDto());
    }
}

[Mapper]
public partial class DepartmentMapper
{
    public partial DepartmentDto ToDto(Department source);
}
```

## Ignoring Properties

Use `IgnoreMember()` to explicitly exclude properties:

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string PasswordHash { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string PasswordHash { get; set; }  // We don't want to map this
}

[Mapper]
public partial class UserMapper
{
    public partial UserDto ToDto(User source);

    public UserMapper()
    {
        IgnoreMember(dto => dto.PasswordHash);
    }
}
```

### When to Use IgnoreMember

- **Optional fields**: Properties that should remain null/default

## Fully Qualified Type Names

Mapgen supports using fully qualified type names alongside type aliases. This is particularly useful when:
- Working with types that have naming conflicts
- Dealing with types from different namespaces with the same name
- Making code more explicit about type origins

### Basic Fully Qualified Names

You can use fully qualified names for any type in your mapper definitions:

```csharp
[Mapper]
public partial class PersonMapper
{
    // Using fully qualified names for clarity
    public partial System.Collections.Generic.List<PersonDto> MapPersonList(
        System.Collections.Generic.List<Person> source);
}

// Generated extension method:
// public static System.Collections.Generic.List<PersonDto> MapPersonList(
//     this System.Collections.Generic.List<Person> source)
```

### Mixing Aliases and Fully Qualified Names

You can mix type aliases with fully qualified names in the same mapper:

```csharp
using System.Collections.Generic;

using PersonResponse = Contracts.PersonDto; 

[Mapper]
public partial class PersonMapper
{
    public partial PersonResponse ToDto(Entities.Person source);
}
```

## Extension Methods

Mapgen automatically generates extension methods for all your mappers:

```csharp
[Mapper]
public partial class CarMapper
{
    public partial CarDto ToCarDto(Car source);
}

// Mapgen generates:
public static class CarMapperExtensions
{
    public static CarDto ToCarDto(this Car source)
    {
        var mapper = new CarMapper();
        return mapper.ToCarDto(source);
    }
}

// Usage:
var carDto = car.ToCarDto();  // Clean and fluent!
```

### Extension Methods with Multiple Parameters

```csharp
[Mapper]
public partial class CarMapper
{
    public partial CarDto ToCarDto(Car source, Driver driver);
}

// Mapgen generates:
public static CarDto ToCarDto(this Car source, Driver driver)

// Usage:
var carDto = car.ToCarDto(driver);
```

### Benefits of Extension Methods

- **Cleaner syntax**: No need to instantiate mappers
- **Discoverable**: IntelliSense shows mapping methods on source objects

## Next Steps

- Explore [Advanced Usage](advanced-usage.md)
- Review [Best Practices](migration/best-practices.md)
- Compare with [other mapping libraries](comparison.md)

