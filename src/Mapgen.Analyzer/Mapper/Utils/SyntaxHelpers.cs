using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mapgen.Analyzer.Mapper.Utils;

/// <summary>
/// Helper methods for common syntax operations.
/// </summary>
internal static class SyntaxHelpers
{
  /// <summary>
  /// Finds the first constructor in a class declaration.
  /// </summary>
  public static ConstructorDeclarationSyntax? FindConstructor(ClassDeclarationSyntax classDeclaration)
  {
    return classDeclaration.Members
      .OfType<ConstructorDeclarationSyntax>()
      .FirstOrDefault();
  }

  /// <summary>
  /// Extracts property name from a lambda expression like: x => x.Property
  /// </summary>
  public static string? ExtractDestinationPropertyName(ExpressionSyntax expression)
  {
    if (expression is SimpleLambdaExpressionSyntax { ExpressionBody: MemberAccessExpressionSyntax memberAccess })
    {
      return memberAccess.Name.Identifier.Text;
    }

    return null;
  }

  /// <summary>
  /// Extracts using directives from a syntax node, including aliases.
  /// </summary>
  public static IReadOnlyList<string> ExtractUsings(SyntaxNode node)
  {
    var usings = new List<string>();

    // Navigate up to find the compilation unit (root of the syntax tree)
    var compilationUnit = node.AncestorsAndSelf()
      .OfType<CompilationUnitSyntax>()
      .FirstOrDefault();

    if (compilationUnit is null)
    {
      return usings;
    }

    foreach (var usingDirective in compilationUnit.Usings)
    {
      // Check if this is an alias (e.g., using CarConstants = Namespace.Type)
      if (usingDirective.Alias is not null && usingDirective.Name is not null)
      {
        var aliasName = usingDirective.Alias.Name.ToString();
        var targetName = usingDirective.Name.ToString();
        usings.Add($"{aliasName} = {targetName}");
        continue;
      }

      if (usingDirective.Name is not null)
      {
        // Regular using directive
        var usingName = usingDirective.Name.ToString();
        usings.Add(usingName);
      }
    }

    return usings;
  }

  /// <summary>
  /// Finds the method declaration syntax for a given method symbol.
  /// </summary>
  public static MethodDeclarationSyntax? FindMethodDeclaration(IMethodSymbol methodSymbol)
  {
    var syntaxReference = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault();
    return syntaxReference?.GetSyntax() as MethodDeclarationSyntax;
  }
}
