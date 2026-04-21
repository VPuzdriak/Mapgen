using Microsoft.CodeAnalysis;

namespace Mapgen.Analyzer.Extensions;

public sealed class ParameterInfo
{
  private static readonly SymbolDisplayFormat _fullyQualifiedFormatWithNullability = new(
    globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
    typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
    genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
    miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                          SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                          SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

  public IParameterSymbol Symbol { get; }
  public string TypeSyntax { get; }
  public string TypeFullyQualifiedSyntax { get; }
  public string Name => Symbol.Name;

  public ParameterInfo(IParameterSymbol symbol, string typeSyntax)
  {
    Symbol = symbol;
    TypeSyntax = typeSyntax;
    TypeFullyQualifiedSyntax = symbol.Type.ToDisplayString(_fullyQualifiedFormatWithNullability);
  }
}
