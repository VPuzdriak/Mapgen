using System.Collections.Generic;
using System.Linq;

using Mapgen.Analyzer.Mapper.Diagnostics;
using Mapgen.Analyzer.Mapper.Metadata;
using Mapgen.Analyzer.Mapper.Utils;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mapgen.Analyzer.Mapper.Strategies;

/// <summary>
/// Base class for all mapping strategies, providing common functionality for finding
/// mapper constructor and method invocations.
/// </summary>
public abstract class BaseMappingStrategy
{
  /// <summary>
  /// Gets the mapper constructor from the class node, or null if not found or invalid.
  /// </summary>
  /// <param name="classNode">The class syntax node to search.</param>
  /// <returns>The constructor declaration, or null if not found.</returns>
  protected static ConstructorDeclarationSyntax? GetMapperConstructor(Microsoft.CodeAnalysis.SyntaxNode classNode)
  {
    if (classNode is not ClassDeclarationSyntax classDeclaration)
    {
      return null;
    }

    return SyntaxHelpers.FindConstructor(classDeclaration);
  }

  /// <summary>
  /// Finds all invocations of a specific method within the mapper constructor.
  /// </summary>
  /// <param name="constructor">The constructor to search.</param>
  /// <param name="methodName">The name of the method to find (e.g., "MapMember", "UseConstructor").</param>
  /// <returns>Collection of invocation expressions matching the method name.</returns>
  protected static IEnumerable<InvocationExpressionSyntax> FindMethodInvocations(
    ConstructorDeclarationSyntax? constructor,
    string methodName)
  {
    if (constructor?.Body is null)
    {
      return Enumerable.Empty<InvocationExpressionSyntax>();
    }

    return constructor.Body.Statements
      .OfType<ExpressionStatementSyntax>()
      .Select(es => es.Expression)
      .OfType<InvocationExpressionSyntax>()
      .Where(inv => IsMethodInvocation(inv, methodName));
  }

  /// <summary>
  /// Checks if an invocation expression is calling a specific method (handles both generic and non-generic).
  /// </summary>
  private static bool IsMethodInvocation(InvocationExpressionSyntax invocation, string methodName)
  {
    return invocation.Expression switch
    {
      // Non-generic: MethodName(...)
      IdentifierNameSyntax { Identifier.Text: var name } => name == methodName,
      // Generic: MethodName<T1, T2>(...)
      GenericNameSyntax { Identifier.Text: var genericName } => genericName == methodName,
      _ => false
    };
  }

  /// <summary>
  /// Validates that a lambda expression uses expression body (not block body).
  /// Lambda blocks are not supported and will generate a diagnostic.
  /// </summary>
  /// <param name="expression">The expression to validate.</param>
  /// <param name="propertyName">The destination property name (for diagnostic).</param>
  /// <param name="methodName">The method name where this lambda is used (for diagnostic).</param>
  /// <param name="location">The location of the method call (for diagnostic).</param>
  /// <param name="methodMetadata">The mapper method metadata to add diagnostics to.</param>
  /// <returns>True if validation passes (expression body), false if validation fails (block body).</returns>
  protected static bool ValidateLambdaExpressionBody(
    ExpressionSyntax expression,
    string propertyName,
    string methodName,
    Location location,
    MapperMethodMetadata methodMetadata)
  {
    // Check if it's a lambda block (not supported)
    if (expression is SimpleLambdaExpressionSyntax { Block: not null }
        or ParenthesizedLambdaExpressionSyntax { Block: not null })
    {
      var diagnostic = MapperDiagnostic.LambdaBlockNotSupported(
        location,
        propertyName,
        methodName);
      methodMetadata.AddDiagnostic(diagnostic);
      return false;
    }

    return true;
  }
}
