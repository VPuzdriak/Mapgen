using System.Collections.Generic;
using System.Linq;

using Mapgen.Analyzer.Mapper.Metadata;
using Mapgen.Analyzer.Mapper.Utils;

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mapgen.Analyzer.Mapper.Strategies;

/// <summary>
/// Helper class for extracting and transforming lambda parameter names in expressions.
/// </summary>
public static class LambdaParameterReplacer
{
  /// <summary>
  /// Extracts the expression body from a lambda and replaces lambda parameter names with actual method parameter names.
  /// </summary>
  /// <param name="lambdaExpression">The lambda expression to process.</param>
  /// <param name="methodMetadata">The mapper method metadata containing actual parameter names.</param>
  /// <param name="skipFirstLambdaParams">Number of lambda parameters to skip (e.g., skip collection item parameter).</param>
  /// <returns>The expression body with replaced parameter names, or null if extraction fails.</returns>
  public static string? ExtractAndReplaceParameters(
    ExpressionSyntax lambdaExpression,
    MapperMethodMetadata methodMetadata,
    int skipFirstLambdaParams = 0)
  {
    string? bodyExpression;
    var lambdaParams = new List<string>();

    // Extract lambda parameters and body
    switch (lambdaExpression)
    {
      case SimpleLambdaExpressionSyntax { ExpressionBody: not null } simpleLambda:
        bodyExpression = simpleLambda.ExpressionBody.ToString();
        lambdaParams.Add(simpleLambda.Parameter.Identifier.Text);
        break;

      case ParenthesizedLambdaExpressionSyntax { ExpressionBody: not null } parenthesizedLambda:
        bodyExpression = parenthesizedLambda.ExpressionBody.ToString();
        lambdaParams.AddRange(parenthesizedLambda.ParameterList.Parameters.Select(p => p.Identifier.Text));
        break;

      default:
        return null;
    }

    // Replace lambda parameter names with actual method parameter names
    // Skip the first N lambda parameters (e.g., collection item)
    // But map the remaining lambda params to method params starting from index 0
    return ReplaceParameters(bodyExpression, lambdaParams, methodMetadata.Parameters, skipFirstLambdaParams);
  }

  /// <summary>
  /// Replaces parameter names in an expression string.
  /// </summary>
  /// <param name="expression">The expression to transform.</param>
  /// <param name="lambdaParams">All lambda parameter names.</param>
  /// <param name="methodParams">All method parameters.</param>
  /// <param name="skipFirstLambdaParams">Number of lambda parameters to skip.</param>
  private static string ReplaceParameters(
    string expression,
    List<string> lambdaParams,
    IReadOnlyList<MapperMethodParameter> methodParams,
    int skipFirstLambdaParams)
  {
    var result = expression;

    // Start from skipFirstLambdaParams in lambda params
    // Map to method params starting from index 0
    for (int lambdaIndex = skipFirstLambdaParams; lambdaIndex < lambdaParams.Count; lambdaIndex++)
    {
      var methodIndex = lambdaIndex - skipFirstLambdaParams;

      if (methodIndex >= methodParams.Count)
      {
        break;
      }

      var lambdaParamName = lambdaParams[lambdaIndex];
      var methodParamName = methodParams[methodIndex].Name;

      // Only replace if names are different
      if (lambdaParamName != methodParamName)
      {
        result = ParameterNameReplacer.ReplaceParameterName(result, lambdaParamName, methodParamName);
      }
    }

    return result;
  }
}
