using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace Mapgen.Analyzer.Mapper.Utils;

/// <summary>
/// Utility methods for working with type symbols.
/// </summary>
public static class TypeSymbolExtensions
{
  extension(INamedTypeSymbol typeSymbol)
  {
    /// <summary>
    /// Gets all properties from a type, including those inherited from base classes.
    /// Properties are returned in declaration order: base class properties first (in their declaration order),
    /// then derived class properties (in their declaration order).
    /// </summary>
    public IEnumerable<IPropertySymbol> GetAllProperties()
    {
      var typeHierarchy = new Stack<INamedTypeSymbol>();
      var currentType = typeSymbol;

      // Build the type hierarchy from derived to base
      while (currentType != null)
      {
        typeHierarchy.Push(currentType);
        currentType = currentType.BaseType;
      }

      // Collect properties from base to derived, preserving declaration order within each type
      var properties = new List<IPropertySymbol>();
      foreach (var type in typeHierarchy)
      {
        var currentProperties = type.GetMembers()
          .OfType<IPropertySymbol>()
          .Where(p => !p.IsStatic) // Exclude static properties
          .OrderBy(p => p.Locations.FirstOrDefault()?.SourceSpan.Start ?? 0); // Preserve declaration order

        properties.AddRange(currentProperties);
      }

      return properties;
    }

    /// <summary>
    /// Gets all mappable members (properties and fields) from a type, including those inherited from base classes.
    /// Members are returned in declaration order: base class members first, then derived class members.
    /// Properties and fields are interleaved based on their declaration order.
    /// </summary>
    public IEnumerable<MemberInfo> GetAllMembers()
    {
      var typeHierarchy = new Stack<INamedTypeSymbol>();
      var currentType = typeSymbol;

      // Build the type hierarchy from derived to base
      while (currentType != null)
      {
        typeHierarchy.Push(currentType);
        currentType = currentType.BaseType;
      }

      // Collect members from base to derived, preserving declaration order within each type
      var members = new List<MemberInfo>();
      foreach (var type in typeHierarchy)
      {
        var currentMembers = type.GetMembers()
          .Where(m =>
          {
            if (m is IPropertySymbol property)
            {
              return !property.IsStatic;
            }
            if (m is IFieldSymbol field)
            {
              return field is { IsStatic: false, IsConst: false };
            }
            return false;
          })
          .OrderBy(m => m.Locations.FirstOrDefault()?.SourceSpan.Start ?? 0) // Preserve declaration order
          .Select(m => m is IPropertySymbol property
            ? MemberInfo.FromProperty(property)
            : MemberInfo.FromField((IFieldSymbol)m));

        members.AddRange(currentMembers);
      }

      return members;
    }
  }
}
