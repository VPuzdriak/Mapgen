# Comparison: Mapgen vs Other Mapping Libraries

This document compares Mapgen with popular .NET mapping libraries: AutoMapper, Mapster, and Mapperly.

## Quick Comparison Table

| Feature                                        | Mapgen            | AutoMapper       | Mapster        | Mapperly         |
|------------------------------------------------|-------------------|------------------|----------------|------------------|
| **Approach**                                   | Source Generator  | Runtime/Compiled | Runtime/Compiled | Source Generator |
| **Performance**                                | Native            | Good             | Very Good        | Native           |
| **IDE Support**                                | ✅ Full            | ❌ Limited        | ❌ Limited      | ✅ Full           |
| **Learning Curve**                             | Easy              | Complex          | Medium         | Easy             |
| **Type Safety**                                | ✅ Compile-time    | ⚠️ Runtime       | ⚠️ Runtime     | ✅ Compile-time   |
| **Debugging**                                  | ✅ Easy            | ❌ Difficult      | ⚠️ Moderate    | ✅ Easy           |
| **Setup Complexity**                           | Simple            | Complex          | Moderate       | Simple           |
| **Multi-param Support**                        | ✅ Yes             | ❌ None           | ❌ None        | ❌ None           |
| **Constructor Mapping**                        | ✅ Auto (single)   | ⚠️ Manual        | ⚠️ Manual      | ✅ Auto (best-fit) |
| **Collection Mapping**                         | ✅ Include mappers | ✅ Built-in       | ✅ Built-in     | ✅ Built-in       |
| **Extension Methods**                          | ✅ Auto-generated  | ❌ Manual         | ⚠️ Optional    | ⚠️ Varies        |
| **DI Integration**                             | Not required       | Complex          | Moderate       | Simple           |
| **Missing properties validation**              | ✅ Compile-time    | ❌ Manual         | ❌ Manual      | ❌ Manual         |
| **Package Size**                               | Small (single)     | Large            | Medium         | Small            |

## Detailed Comparison

## 1. AutoMapper

### Overview
AutoMapper is the most popular and mature mapping library in .NET, using reflection.

**Official Documentation:** https://docs.automapper.org/  
**GitHub:** https://github.com/AutoMapper/AutoMapper  
**NuGet:** https://www.nuget.org/packages/AutoMapper

### AutoMapper Approach
```csharp
// Configuration class
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.FullName, 
                       opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
            .ForMember(dest => dest.Age, 
                       opt => opt.MapFrom(src => CalculateAge(src.BirthDate)));
    }
    
    private static int CalculateAge(DateTime birthDate) { /* ... */ }
}

// DI Configuration
services.AddAutoMapper(typeof(MappingProfile));

// Usage
public class UserService
{
    private readonly IMapper _mapper;
    
    public UserService(IMapper mapper)
    {
        _mapper = mapper;
    }
    
    public UserDto GetUser(User user)
    {
        return _mapper.Map<UserDto>(user);
    }
}
```

### Mapgen Approach
```csharp
[Mapper]
public partial class UserMapper
{
    public partial UserDto ToDto(User source);

    public UserMapper()
    {
        MapMember(dto => dto.FullName, 
                  user => $"{user.FirstName} {user.LastName}");
        MapMember(dto => dto.Age, 
                  user => CalculateAge(user.BirthDate));
    }
    
    private static int CalculateAge(DateTime birthDate) { /* ... */ }
}

// Usage
var userDto = user.ToDto();  // Extension method
// or
var mapper = new UserMapper();
var userDto = mapper.ToDto(user);
```

### Key Differences

| Aspect | AutoMapper | Mapgen                |
|--------|------------|-----------------------|
| **Configuration** | Separate Profile classes | In-mapper constructor |
| **Runtime Behavior** | Reflection + compilation | Pure compile-time     |
| **Constructor Mapping** | Manual ConstructUsing() | Automatic detection   |
| **Dependency Injection** | Required for most patterns | None                  |
| **Error Detection** | Runtime | Compile-time          |
| **Debugging** | Difficult (expression trees) | Easy (plain C# code)  |
| **Go-to-Definition** | Not available | Fully supported       |

#### Constructor Mapping Comparison

**AutoMapper:**
```csharp
CreateMap<Product, ProductDto>()
    .ConstructUsing(src => new ProductDto(src.Name, src.Price));
```
- Requires explicit `ConstructUsing()` configuration
- No automatic detection
- Must manually specify all constructor parameters

**Mapgen:**
```csharp
[Mapper]
public partial class ProductMapper
{
  public partial ProductDto ToDto(Product source);
  // Constructor automatically detected when:
  // - Single constructor exists
  // - Parameters match source properties
  // - Types are compatible
}
```
- Automatic detection for single constructors
- Matches parameters by name (case-insensitive)
- Supports implicit type conversions (e.g., int → long)
- Shows clear error for ambiguous cases (2+ constructors)

### Why Choose Mapgen Over AutoMapper?

✅ **Better Performance**: No reflection, no runtime compilation overhead  
✅ **Simpler Setup**: No DI configuration required  
✅ **Better IDE Support**: Full IntelliSense, go-to-definition, find references  
✅ **Easier Debugging**: Generated code is readable C#  
✅ **Type Safety**: Errors caught at compile time  
✅ **Less Magic**: Explicit, understandable code generation  
✅ **Compile-type check**: Missed properties caught at compile time

### When AutoMapper Might Be Better?

- Large existing codebase heavily invested in AutoMapper
- Need for advanced features like value converters, resolvers, and type converters
- Dynamic mapping scenarios at runtime

---

## 2. Mapster

### Overview
Mapster is a fast, convention-based mapper with optional code generation support.

**Official Documentation:** https://github.com/MapsterMapper/Mapster/wiki  
**GitHub:** https://github.com/MapsterMapper/Mapster  
**NuGet:** https://www.nuget.org/packages/Mapster

### Mapster Approach
```csharp
// Configuration (optional)
TypeAdapterConfig<User, UserDto>
    .NewConfig()
    .Map(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}")
    .Map(dest => dest.Age, src => CalculateAge(src.BirthDate));

// Usage
var userDto = user.Adapt<UserDto>();

// Or with code generation
[AdaptTo(typeof(UserDto))]
public class User { /* ... */ }
```

### Mapgen Approach
```csharp
[Mapper]
public partial class UserMapper
{
    public partial UserDto ToDto(User source);

    public UserMapper()
    {
        MapMember(dto => dto.FullName, 
                  user => $"{user.FirstName} {user.LastName}");
        MapMember(dto => dto.Age, user => CalculateAge(user.BirthDate));
    }
}

var userDto = user.ToDto();
```

### Key Differences

| Aspect | Mapster | Mapgen |
|--------|---------|--------|
| **Default Mode** | Runtime (fast) | Compile-time |
| **Configuration** | Global or per-type | Per-mapper class |
| **Constructor Mapping** | Manual ConstructUsing() | Automatic detection |
| **Code Generation** | Optional add-on | Core feature |
| **Syntax** | Adapt<T>() | Explicit mapper methods |
| **Type Attributes** | Required for codegen | Not needed |

#### Constructor Mapping Comparison

**Mapster:**
```csharp
TypeAdapterConfig<Product, ProductDto>
    .NewConfig()
    .ConstructUsing(src => new ProductDto(src.Name, src.Price));
```
- Requires `ConstructUsing()` configuration
- Global configuration affects all instances
- No automatic parameter matching

**Mapgen:**
```csharp
[Mapper]
public partial class ProductMapper
{
  public partial ProductDto ToDto(Product source);
  // Automatically uses constructor when single constructor exists
}
```
- Zero configuration for simple cases
- Automatic parameter matching by name
- Compile-time validation

### Why Choose Mapgen Over Mapster?

✅ **True Compile-Time**: Always compile-time, not optional  
✅ **Better Organization**: Mappers as dedicated classes  
✅ **Cleaner Syntax**: No generic `Adapt<T>()` calls  
✅ **No Global State**: All configuration is local to mappers  
✅ **Compile-type check**: Missed properties caught at compile time

### When Mapster Might Be Better?

- Need extremely fast prototyping
- Prefer convention over configuration
- Want more dynamic mapping scenarios
- Already familiar with Mapster's API

---

## 3. Mapperly

### Overview
Mapperly is a source generator-based mapper similar to Mapgen, using attributes for configuration.

**Official Documentation:** https://mapperly.riok.app/  
**GitHub:** https://github.com/riok/mapperly  
**NuGet:** https://www.nuget.org/packages/Riok.Mapperly

### Mapperly Approach
```csharp
[Mapper]
public partial class UserMapper
{
    [MapProperty(nameof(User.FirstName) + " + ' ' + " + nameof(User.LastName), 
                 nameof(UserDto.FullName))]
    [MapProperty(nameof(User.BirthDate), nameof(UserDto.Age), 
                 Use = nameof(CalculateAge))]
    public partial UserDto ToDto(User source);
    
    private int CalculateAge(DateTime birthDate) { /* ... */ }
}

var userDto = mapper.ToDto(user);
```

### Mapgen Approach
```csharp
[Mapper]
public partial class UserMapper
{
    public partial UserDto ToDto(User source);

    public UserMapper()
    {
        MapMember(dto => dto.FullName, 
                  user => $"{user.FirstName} {user.LastName}");
        MapMember(dto => dto.Age, user => CalculateAge(user.BirthDate));
    }
    
    private static int CalculateAge(DateTime birthDate) { /* ... */ }
}

var userDto = user.ToDto();
```

### Key Differences

| Aspect | Mapperly | Mapgen |
|--------|----------|--------|
| **Configuration Style** | Attributes on methods | Code in constructor |
| **Constructor Mapping** | Auto (best-fit) | Auto (single only) |
| **Syntax** | String-based or attributes | Lambda expressions |
| **Type Safety** | Moderate (some strings) | Full (no strings) |
| **Readability** | Attributes can be verbose | Clean, fluent code |
| **Extension Methods** | Generated per config | Always generated |

#### Constructor Mapping Comparison

**Mapperly:**
```csharp
[Mapper]
public partial class ProductMapper
{
    public partial ProductDto ToDto(Product source);
    // Automatically selects constructor that can map the most properties
}
```
- **Automatic best-fit selection** - picks constructor with most mappable parameters
- Works even with multiple constructors
- Can override with `[MapperConstructor]` attribute if needed
- Prioritizes constructors that maximize property coverage

**Mapgen:**
```csharp
[Mapper]
public partial class ProductMapper
{
  public partial ProductDto ToDto(Product source);
  // Automatically uses constructor when single constructor exists
}
```
- **Automatic for single constructor** - zero configuration when unambiguous
- Shows error for multiple constructors (requires explicit choice)
- Matches parameters by name (case-insensitive)
- Optional `UseConstructor()` for explicit selection or transformations

**Key Difference:**
- **Mapperly**: Automatically picks best constructor even with multiple options
- **Mapgen**: Requires explicit choice when multiple constructors exist (prevents ambiguity)

### Why Choose Mapgen Over Mapperly?

✅ **Better Syntax**: Lambda expressions vs attributes/strings  
✅ **More Readable**: Configuration reads like normal C# code  
✅ **Better IntelliSense**: Full IDE support in lambda expressions  
✅ **Less Verbose**: Constructor-based config is cleaner  
✅ **Extension Methods**: Always generated automatically  
✅ **Compile-type check**: Missed properties caught at compile time

### When Mapperly Might Be Better?

- Prefer attribute-based configuration
- Need per-method mapping customization
- Want more granular control per mapping method

---

## Feature Spotlight: Constructor Mapping

Constructor mapping is essential for working with immutable objects, readonly properties, and domain-driven design patterns. Here's how each library handles it:

### Comparison Summary

| Library | Approach | Auto-Detection | Configuration | Ambiguity Handling |
|---------|----------|----------------|---------------|-------------------|
| **Mapgen** | Automatic (single) | ✅ Yes | Zero for single ctor | Compile-time error for 2+ |
| **AutoMapper** | Manual | ❌ No | ConstructUsing() | Runtime |
| **Mapster** | Manual | ❌ No | ConstructUsing() | Runtime |
| **Mapperly** | Automatic (best-fit) | ✅ Yes | Zero config | Picks most mappable |

### Example Scenario: Mapping to Immutable DTO

```csharp
// Destination with readonly properties
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
```

#### Mapgen (Zero Configuration) ⭐
```csharp
[Mapper]
public partial class ProductMapper
{
  public partial ProductDto ToDto(Product source);
  // Done! Constructor automatically detected and used
}
```

#### AutoMapper (Manual Configuration)
```csharp
public class MappingProfile : Profile
{
  public MappingProfile()
  {
    CreateMap<Product, ProductDto>()
      .ConstructUsing(src => new ProductDto(src.Name, src.Price));
  }
}
```

#### Mapster (Manual Configuration)
```csharp
TypeAdapterConfig<Product, ProductDto>
  .NewConfig()
  .ConstructUsing(src => new ProductDto(src.Name, src.Price));
```

#### Mapperly (Zero Configuration - Best-Fit) ⭐
```csharp
[Mapper]
public partial class ProductMapper
{
  public partial ProductDto ToDto(Product source);
  // Done! Automatically picks best-fit constructor
}
```

### Constructor Selection Approaches

**Mapgen:**
- ✅ **Automatic for single constructor** - zero config when unambiguous
- ❌ **Error for multiple constructors** - requires explicit choice (prevents ambiguity)
- ✅ **Explicit control** - `UseConstructor()` for custom selection/transformations

**Mapperly:**
- ✅ **Automatic best-fit** - picks constructor with most mappable parameters
- ✅ **Smart selection** - works even with multiple constructors
- ✅ **Override available** - `[MapperConstructor]` attribute for explicit control

### Trade-offs

**Mapgen Approach:**
- **Pros:** Prevents ambiguity, explicit when multiple constructors exist
- **Cons:** Requires configuration when multiple constructors present

**Mapperly Approach:**
- **Pros:** Zero config even with multiple constructors, smart best-fit selection
- **Cons:** Less explicit, may pick unexpected constructor in complex scenarios

### Mapgen's Other Advantages

✅ **Lambda expressions** - Clean, type-safe configuration syntax
✅ **Smart matching** - Matches parameters by name (case-insensitive)  
✅ **Type-safe** - Supports implicit conversions (int → long, etc.)  
✅ **Multi-parameter mapping** - Map from multiple source objects
✅ **Extension methods** - Always auto-generated

**When you need customization:**
```csharp
public ProductMapper()
{
  UseConstructor(
    source => source.Name.ToUpper(),  // Transformation
    source => source.Price * 1.1m     // Calculation
  );
}
```

---


## Decision Matrix

### Choose **Mapgen** if you want:
- ✅ True compile-time code generation
- ✅ Automatic constructor detection and mapping
- ✅ Clean, readable configuration with lambda expressions
- ✅ Excellent IDE support (IntelliSense, go-to-definition)
- ✅ Automatic extension methods
- ✅ Multi-parameter mapping support
- ✅ Separation of mapping from domain models
- ✅ Easy debugging with readable generated code
- ✅ Compile-time validation of missing properties

### Choose **AutoMapper** if you need:
- To support existing codebase with AutoMapper
- Advanced features (value converters, custom resolvers)
- Dynamic runtime mapping scenarios
- Extensive documentation and community support

### Choose **Mapster** if you want:
- Fastest prototyping with conventions
- Global configuration options
- Mix of runtime and compile-time mapping
- Very terse syntax

### Choose **Mapperly** if you prefer:
- Attribute-based configuration
- Per-method mapping customization
- Compile-time generation with attribute syntax

---

## Migration Resources

Ready to switch to Mapgen? Check our migration guides:

- [Migration from AutoMapper](migration/from-automapper.md)
- [Migration from Mapster](migration/from-mapster.md)
- [Migration from Mapperly](migration/from-mapperly.md)

## Next Steps

- Review [Best Practices](migration/best-practices.md)
- Learn about [Core Features](core-features.md)
- Explore [Advanced Usage](advanced-usage.md)
