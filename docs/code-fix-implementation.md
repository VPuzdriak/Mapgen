# Code Fix Provider Implementation

## Overview
Implemented code fix provider for MAPPER001 diagnostic (unmapped property) to improve developer experience by offering quick fix options directly in the IDE.

## Features

### 1. Enhanced Diagnostics
- Added `PropertyName` property to `MapperDiagnostic` class to store unmapped property name
- Updated `MapperDiagnosticsReporter` to pass property name in diagnostic properties using `ImmutableDictionary`
- Property name is now available for code fix providers to use

### 2. Code Fix Provider
Created `UnmappedPropertyCodeFixProvider` that:
- Registers for `MAPPER001` diagnostic ID
- Extracts property name from diagnostic properties
- Provides a parent code action titled "Fix {PropertyName} member" with two nested options:
  1. **Add MapMember with TODO** - Adds `MapMember(dest => dest.PropertyName, src => src.TODO)` where `TODO` is marked with `RenameAnnotation` for immediate editing
  2. **Add IgnoreMember** - Adds `IgnoreMember(dest => dest.PropertyName)`

### 3. Constructor Management
- Automatically finds existing parameterless constructor or creates one
- Skips fix if constructor has parameters (invalid per MAPPER010)
- New constructor is inserted after the first mapping method
- Proper indentation (2 spaces) matching project style

### 4. Cursor Positioning
- Uses `RenameAnnotation.Create()` on the `TODO` identifier
- When "Add MapMember with TODO" is applied, the IDE automatically enters rename mode
- User can immediately type to replace `TODO` with the actual mapping expression
- Provides seamless editing experience

## Implementation Details

### Files Modified
1. **MapperDiagnostic.cs**
   - Added `PropertyName` property
   - Updated constructor to accept optional `propertyName` parameter
   - Modified `MissingPropertyMapping` factory to pass property name

2. **MapperDiagnosticsReporter.cs**
   - Added `System.Collections.Immutable` using directive
   - Updated `Report` method to pass property name in diagnostic properties

3. **Mapgen.Analyzer.csproj**
   - Added `Microsoft.CodeAnalysis.CSharp.Workspaces` v5.0.0 package reference

### Files Created
1. **UnmappedPropertyCodeFixProvider.cs**
   - Full implementation of code fix provider
   - Nested code actions for MapMember and IgnoreMember options
   - Syntax tree manipulation using `SyntaxFactory`
   - Constructor creation and modification logic

## Usage Example

Given the following mapper with unmapped property:

```csharp
[Mapper]
public partial class CarOwnerMapper
{
  public partial CarOwnerDto ToCarOwnerDto(CarOwner source);
}
```

When MAPPER001 diagnostic appears for `FullName` property:
1. IDE shows lightbulb/quick fix icon
2. Click shows "Fix FullName member" with submenu:
   - "Add MapMember with TODO"
   - "Add IgnoreMember"

### Option 1: Add MapMember with TODO
Generates:
```csharp
[Mapper]
public partial class CarOwnerMapper
{
  public partial CarOwnerDto ToCarOwnerDto(CarOwner source);

  public CarOwnerMapper()
  {
    MapMember(dest => dest.FullName, src => src.TODO);
  }
}
```
- `TODO` is automatically selected in rename mode
- User types replacement expression, e.g., `$"{src.FirstName} {src.LastName}"`

### Option 2: Add IgnoreMember
Generates:
```csharp
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

## Technical Notes

### RS1038 Warning
The implementation includes `Microsoft.CodeAnalysis.Workspaces` which triggers RS1038 warnings on source generator classes. This is expected and acceptable:
- Code fix providers require Workspaces APIs
- Code fixes work in IDE scenarios, not command-line builds
- Warnings are informational, not errors
- Package is marked with `PrivateAssets="all"`

### Limitations
- No batch fix support (FixAllProvider returns null)
- Each unmapped property requires individual fix application
- This is intentional to give developers control over each mapping decision

### Benefits
- Reduces manual typing and syntax errors
- Provides consistent code style
- Immediate feedback and correction
- Improves developer productivity
- Lowers barrier to entry for new users

## Testing
Build the sample project to see the diagnostic:
```bash
cd /Volumes/My\ Data/Projects/Pet/Mapgen
dotnet build src/Mapgen.Sample.Console/Mapgen.Sample.Console.csproj
```

The MAPPER001 diagnostic will appear with the code fix hint available in the IDE.

