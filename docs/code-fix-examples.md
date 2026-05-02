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

## Constructor Selection Code Fix (MAPPER008)

When a destination type has multiple constructors and none is explicitly specified, the analyzer shows MAPPER008 diagnostic with a code fix to help you choose which constructor to use.

### Before - With Diagnostic Error

```csharp
using Mapgen.Analyzer;

[Mapper]
public partial class CustomerMapper
{
  public partial CustomerDto ToDto(Customer source);
  //                         ^ MAPPER008 error appears here
  // Error: Cannot generate mapping to "CustomerDto". Type has multiple constructors. 
  //        Use UseConstructor() to specify which constructor parameters to use, 
  //        or UseEmptyConstructor() to use the parameterless constructor.
}
```

### Destination Type with Multiple Constructors

```csharp
public class CustomerDto
{
  public string Name { get; set; }
  public string Email { get; set; }
  public int Age { get; set; }

  // Parameterless constructor
  public CustomerDto() { }

  // Parameterized constructor 1
  public CustomerDto(string name, string email) { }

  // Parameterized constructor 2
  public CustomerDto(string name, string email, int age) { }
}
```

### IDE Experience

When you hover over the error or click the lightbulb icon 💡, you see:

```
🔧 Pick CustomerDto constructor
   ├─ Use empty constructor
   ├─ Use CustomerDto(string name, string email)
   └─ Use CustomerDto(string name, string email, int age)
```

### After - Option 1: Use empty constructor

```csharp
[Mapper]
public partial class CustomerMapper
{
  public partial CustomerDto ToDto(Customer source);

  public CustomerMapper()
  {
    UseEmptyConstructor();
  }
}
```

✅ Properties will be mapped via object initializer.

### After - Option 2: Use CustomerDto(string name, string email)

```csharp
[Mapper]
public partial class CustomerMapper
{
  public partial CustomerDto ToDto(Customer source);

  public CustomerMapper()
  {
    UseConstructor(
      source => source.TODO_name,
      source => source.TODO_email
    );
    //                ^^^^
    //     Cursor here, rename mode active
  }
}
```

### User Action
1. Code fix creates constructor and UseConstructor call
2. First `TODO_` identifier is automatically selected (rename mode)
3. User types the source property name, e.g.: `Name`
4. Press Tab to move to next parameter
5. Type the next source property name, e.g.: `Email`
6. Press Enter or Escape to complete

### Final Result

```csharp
[Mapper]
public partial class CustomerMapper
{
  public partial CustomerDto ToDto(Customer source);

  public CustomerMapper()
  {
    UseConstructor(
      source => source.Name,
      source => source.Email
    );
  }
}
```

✅ Constructor parameters are mapped, remaining properties use object initializer.

### After - Option 3: Use CustomerDto(string name, string email, int age)

```csharp
[Mapper]
public partial class CustomerMapper
{
  public partial CustomerDto ToDto(Customer source);

  public CustomerMapper()
  {
    UseConstructor(
      source => source.TODO_name,
      source => source.TODO_email,
      source => source.TODO_age
    );
    //                ^^^^
    //     Cursor here, rename mode active
  }
}
```

After editing, you can map all properties via constructor:

```csharp
[Mapper]
public partial class CustomerMapper
{
  public partial CustomerDto ToDto(Customer source);

  public CustomerMapper()
  {
    UseConstructor(
      source => source.Name,
      source => source.Email,
      source => source.Age
    );
  }
}
```

✅ All properties mapped via constructor parameters.

