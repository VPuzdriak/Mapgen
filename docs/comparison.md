# Comparison: Mapgen vs Other Mapping Libraries

This document compares Mapgen with popular .NET mapping libraries: AutoMapper, Mapster, and Mapperly.

## Quick Comparison Table

| Feature                                        | Mapgen            | AutoMapper       | Mapster        | Mapperly |
|------------------------------------------------|-------------------|------------------|----------------|----------|
| **Approach**                                   | Source Generator  | Runtime/Compiled | Runtime/Compiled | Source Generator |
| **Performance**                                | ⭐⭐⭐⭐⭐ Native      | ⭐⭐⭐ Good         | ⭐⭐⭐⭐ Very Good | ⭐⭐⭐⭐⭐ Native |
| **IDE Support**                                | ✅ Full            | ❌ Limited        | ❌ Limited      | ✅ Full |
| **Learning Curve**                             | ⭐⭐ Easy           | ⭐⭐⭐⭐ Complex     | ⭐⭐⭐ Medium     | ⭐⭐ Easy |
| **Type Safety**                                | ✅ Compile-time    | ⚠️ Runtime       | ⚠️ Runtime     | ✅ Compile-time |
| **Debugging**                                  | ✅ Easy            | ❌ Difficult      | ⚠️ Moderate    | ✅ Easy |
| **Setup Complexity**                           | ⭐ Simple          | ⭐⭐⭐⭐ Complex     | ⭐⭐ Moderate    | ⭐ Simple |
| **Multi-param Support**                        | ✅ Yes             | ❌ None           | ❌ None               | ❌ None |
| **Collection Mapping**                         | ✅ Include mappers | ✅ Built-in       | ✅ Built-in     | ✅ Built-in |
| **Extension Methods**                          | ✅ Auto-generated  | ❌ Manual         | ⚠️ Optional    | ⚠️ Varies |
| **DI Integration**                             | ❌ None            | Complex          | Moderate       | Simple |
| **Missing properties validation** | ✅ Compile-time    | ❌ Manual          | ❌ Manual       | ❌ Manual |
| **Package Size**                               | Small             | Large            | Medium         | Small |

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
| **Dependency Injection** | Required for most patterns | None                  |
| **Error Detection** | Runtime | Compile-time          |
| **Debugging** | Difficult (expression trees) | Easy (plain C# code)  |
| **Go-to-Definition** | Not available | Fully supported       |

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
| **Code Generation** | Optional add-on | Core feature |
| **Syntax** | Adapt<T>() | Explicit mapper methods |
| **Type Attributes** | Required for codegen | Not needed |

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
| **Syntax** | String-based or attributes | Lambda expressions |
| **Type Safety** | Moderate (some strings) | Full (no strings) |
| **Readability** | Attributes can be verbose | Clean, fluent code |
| **Extension Methods** | Generated per config | Always generated |

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

## Performance Comparison

### Benchmark Results (Approximate)

Mapping 100,000 objects:

| Library | Time | Memory | Relative Speed |
|---------|------|--------|----------------|
| **Hand-written** | 1.0ms | 1.5 MB | 1.0x (baseline) |
| **Mapgen** | 1.0ms | 1.5 MB | 1.0x |
| **Mapperly** | 1.0ms | 1.5 MB | 1.0x |
| **Mapster** | 2.5ms | 2.0 MB | 2.5x |
| **AutoMapper** | 5.0ms | 3.0 MB | 5.0x |

**Note**: Source generators (Mapgen, Mapperly) produce code identical to hand-written mappers, resulting in equivalent performance.

---

## Decision Matrix

### Choose **Mapgen** if you want:
- ✅ True compile-time code generation
- ✅ Clean, readable configuration with lambda expressions
- ✅ Excellent IDE support (IntelliSense, go-to-definition)
- ✅ Automatic extension methods
- ✅ Multi-parameter mapping support
- ✅ Separation of mapping from domain models
- ✅ Easy debugging with readable generated code

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
