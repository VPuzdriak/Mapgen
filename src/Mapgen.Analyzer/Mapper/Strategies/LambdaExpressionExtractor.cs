using Mapgen.Analyzer.Mapper.Metadata;
using Mapgen.Analyzer.Mapper.Utils;

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mapgen.Analyzer.Mapper.Strategies;

/// <summary>
/// Helper class for extracting source expressions from various lambda and method reference syntaxes.
/// </summary>
public static class LambdaExpressionExtractor
{
  /// <summary>
  /// Extracts the source expression from a lambda or method reference expression.
  /// </summary>
  /// <param name="expression">The expression to extract from.</param>
  /// <param name="methodMetadata">The mapper method metadata.</param>
  /// <returns>The extracted source expression, or null if extraction fails.</returns>
  public static string? ExtractSourceExpression(
    ExpressionSyntax expression,
    MapperMethodMetadata methodMetadata)
  {
    return expression switch
    {
      // Handle simple lambda: src => src.Id
      SimpleLambdaExpressionSyntax { ExpressionBody: not null } simpleLambda
        => ExtractFromSimpleLambda(simpleLambda, methodMetadata),

      // Handle parenthesized lambda: (car, driver) => driver.ToDto()
      ParenthesizedLambdaExpressionSyntax { ExpressionBody: not null } parenthesizedLambda
        => ExtractFromParenthesizedLambda(parenthesizedLambda, methodMetadata),

      // Handle method group with member access: CarNameBuilder.GetCarName
      MemberAccessExpressionSyntax memberAccess
        => ConvertMethodGroupToInvocation(memberAccess.ToString(), methodMetadata),

      // Handle method group: GetCarName
      IdentifierNameSyntax identifier
        => ConvertMethodGroupToInvocation(identifier.Identifier.Text, methodMetadata),

      _ => null
    };
  }

  private static string ExtractFromSimpleLambda(
    SimpleLambdaExpressionSyntax lambda,
    MapperMethodMetadata methodMetadata)
  {
    var lambdaParameterName = lambda.Parameter.Identifier.Text;
    var actualParameterName = methodMetadata.SourceObjectParameter.Name;
    var sourceExpression = lambda.ExpressionBody!.ToString();

    // Replace lambda parameter name with actual method parameter name
    return ParameterNameReplacer.ReplaceParameterName(
      sourceExpression,
      lambdaParameterName,
      actualParameterName);
  }

  private static string ExtractFromParenthesizedLambda(
    ParenthesizedLambdaExpressionSyntax lambda,
    MapperMethodMetadata methodMetadata)
  {
    var sourceExpression = lambda.ExpressionBody!.ToString();

    // Replace all lambda parameter names with actual method parameter names
    return LambdaParameterReplacer.ExtractAndReplaceParameters(lambda, methodMetadata, skipFirstLambdaParams: 0)
           ?? sourceExpression;
  }

  private static string ConvertMethodGroupToInvocation(
    string methodName,
    MapperMethodMetadata methodMetadata)
  {
    // Convert method group to method invocation: GetCarName(car)
    // Note: Method groups only pass the first parameter by default
    return $"{methodName}({methodMetadata.SourceObjectParameter.Name})";
  }
}
