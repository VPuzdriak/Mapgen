# Code Fix Usage Example

## Before - With Diagnostic Error

```csharp
using Mapgen.Analyzer;
using Mapgen.Sample.Console.Models;

namespace Mapgen.Sample.Console.Mappers;

[Mapper]
public partial class CarOwnerMapper
{
  public partial CarOwnerDto ToCarOwnerDto(CarOwner source);
  //                         ^ MAPPER001 error appears here
  // Error: "CarOwnerDto" type has "FullName" property which does not 
  //        exist in "CarOwner" type. Please, add custom mapping using 
  //        MapMember() or ignore this property explicitly using IgnoreMember().
}
```

## IDE Experience

When you hover over the error or click the lightbulb icon 💡, you see:

```
🔧 Fix FullName member
   ├─ Add MapMember with TODO
   └─ Add IgnoreMember
```

## After - Option 1: Add MapMember with TODO

```csharp
using Mapgen.Analyzer;
using Mapgen.Sample.Console.Models;

namespace Mapgen.Sample.Console.Mappers;

[Mapper]
public partial class CarOwnerMapper
{
  public partial CarOwnerDto ToCarOwnerDto(CarOwner source);

  public CarOwnerMapper()
  {
    MapMember(dest => dest.FullName, src => src.TODO);
    //                                          ^^^^
    //                               Cursor here, rename mode active
  }
}
```

### User Action
1. Code fix creates constructor and MapMember call
2. `TODO` identifier is automatically selected (rename mode)
3. User types the mapping expression, e.g.: `$"{src.FirstName} {src.LastName}"`
4. Press Enter or Escape to complete

### Final Result
```csharp
using Mapgen.Analyzer;
using Mapgen.Sample.Console.Models;

namespace Mapgen.Sample.Console.Mappers;

[Mapper]
public partial class CarOwnerMapper
{
  public partial CarOwnerDto ToCarOwnerDto(CarOwner source);

  public CarOwnerMapper()
  {
    MapMember(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}");
  }
}
```

✅ No more errors, mapping is complete!

## After - Option 2: Add IgnoreMember

```csharp
using Mapgen.Analyzer;
using Mapgen.Sample.Console.Models;

namespace Mapgen.Sample.Console.Mappers;

[Mapper]
public partial class CarOwnerMapper
{
  public partial CarOwnerDto ToCarOwnerDto(CarOwner source);

  public CarOwnerMapper()
  {
    IgnoreMember(dest => dest.FullName);
  }
}
```

✅ Property is explicitly ignored, no more errors!

## Multiple Unmapped Properties

If you have multiple unmapped properties, each gets its own diagnostic:

```csharp
[Mapper]
public partial class PersonMapper
{
  public partial PersonDto ToDto(Person source);
  // ERROR: "PersonDto" has "FullName" property - 💡 Fix FullName member
  // ERROR: "PersonDto" has "Age" property - 💡 Fix Age member
  // ERROR: "PersonDto" has "DisplayName" property - 💡 Fix DisplayName member
}
```

You can apply the code fix for each property individually:

```csharp
[Mapper]
public partial class PersonMapper
{
  public partial PersonDto ToDto(Person source);

  public PersonMapper()
  {
    MapMember(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}");
    MapMember(dest => dest.Age, src => DateTime.Now.Year - src.BirthYear);
    IgnoreMember(dest => dest.DisplayName);
  }
}
```

## Constructor Already Exists

If constructor already exists, the code fix appends to it:

### Before
```csharp
[Mapper]
public partial class PersonMapper
{
  public partial PersonDto ToDto(Person source);

  public PersonMapper()
  {
    MapMember(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}");
  }
  // ERROR: "PersonDto" has "Age" property - 💡 Fix Age member
}
```

### After (applying "Add MapMember with TODO")
```csharp
[Mapper]
public partial class PersonMapper
{
  public partial PersonDto ToDto(Person source);

  public PersonMapper()
  {
    MapMember(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}");
    MapMember(dest => dest.Age, src => src.TODO);
    //                                      ^^^^
    //                           Cursor here, ready to edit
  }
}
```

## Benefits

1. **Zero typing required** - Constructor and method call generated automatically
2. **Immediate editing** - Cursor positioned at TODO, ready for your expression
3. **IDE integration** - Works with lightbulb/quick fix UI
4. **Type safety** - Generated code is syntactically correct
5. **Consistent style** - Proper indentation and formatting
6. **Flexible choice** - MapMember for custom mapping, IgnoreMember for exclusion
7. **Learn by example** - New users see correct syntax immediately

