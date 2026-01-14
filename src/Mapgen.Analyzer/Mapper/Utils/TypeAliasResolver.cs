using System.Collections.Generic;

using Microsoft.CodeAnalysis;

namespace Mapgen.Analyzer.Mapper.Utils;

/// <summary>
/// Resolves type names to their aliases when available.
/// </summary>
public sealed class TypeAliasResolver
{
  private readonly Dictionary<string, string> _aliasMap = new();

  public TypeAliasResolver(IReadOnlyList<string> usings)
  {
    foreach (var usingDirective in usings)
    {
      // Check if this is an alias (contains '=')
      if (!usingDirective.Contains(" = "))
      {
        continue;
      }

      var parts = usingDirective.Split([" = "], System.StringSplitOptions.RemoveEmptyEntries);
      if (parts.Length == 2)
      {
        var alias = parts[0].Trim();
        var fullName = parts[1].Trim();
        _aliasMap[fullName] = alias;
      }
    }
  }

  /// <summary>
  /// Gets the display string for a type, using an alias if one exists.
  /// </summary>
  public string GetTypeDisplayString(ITypeSymbol typeSymbol)
  {
    var fullName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
      .Replace("global::", ""); // Remove global:: prefix

    // Check if we have an alias for this type
    if (_aliasMap.TryGetValue(fullName, out var alias))
    {
      return alias;
    }

    // Check with namespace qualification
    var namespacedName = typeSymbol.ToDisplayString(new SymbolDisplayFormat(
      typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces
    ));

    if (_aliasMap.TryGetValue(namespacedName, out alias))
    {
      return alias;
    }

    // No alias found, use minimal qualification
    return typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
  }
}
