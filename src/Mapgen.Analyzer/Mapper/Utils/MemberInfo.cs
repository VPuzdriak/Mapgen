using Microsoft.CodeAnalysis;

namespace Mapgen.Analyzer.Mapper.Utils;

/// <summary>
/// Represents a unified view of a property or field for mapping purposes.
/// </summary>
public sealed class MemberInfo
{
  public ISymbol Symbol { get; }
  public string Name { get; }
  public ITypeSymbol Type { get; }
  public bool IsProperty { get; }
  public bool IsField { get; }

  private MemberInfo(ISymbol symbol, string name, ITypeSymbol type, bool isProperty, bool isField)
  {
    Symbol = symbol;
    Name = name;
    Type = type;
    IsProperty = isProperty;
    IsField = isField;
  }

  public static MemberInfo FromProperty(IPropertySymbol property)
  {
    return new MemberInfo(property, property.Name, property.Type, isProperty: true, isField: false);
  }

  public static MemberInfo FromField(IFieldSymbol field)
  {
    return new MemberInfo(field, field.Name, field.Type, isProperty: false, isField: true);
  }

  public IPropertySymbol? AsProperty() => IsProperty ? (IPropertySymbol)Symbol : null;
  public IFieldSymbol? AsField() => IsField ? (IFieldSymbol)Symbol : null;

  public bool IsReadable()
  {
    if (IsProperty)
    {
      return ((IPropertySymbol)Symbol).IsReadable();
    }
    if (IsField)
    {
      return ((IFieldSymbol)Symbol).IsReadable();
    }
    return false;
  }

  public bool IsSettable()
  {
    if (IsProperty)
    {
      return ((IPropertySymbol)Symbol).IsSettable();
    }
    if (IsField)
    {
      return ((IFieldSymbol)Symbol).IsWritable();
    }
    return false;
  }

  public bool IsRequired()
  {
    if (IsProperty)
    {
      return ((IPropertySymbol)Symbol).IsRequired;
    }
    if (IsField)
    {
      return ((IFieldSymbol)Symbol).IsRequired;
    }
    return false;
  }
}
