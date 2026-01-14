using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Mapgen.Analyzer.Mapper.Diagnostics;
using Mapgen.Analyzer.Mapper.MappingDescriptors;
using Mapgen.Analyzer.Mapper.Metadata;
using Mapgen.Analyzer.Mapper.Utils;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mapgen.Analyzer.Mapper.Strategies;

public sealed class CustomMappingStrategy
{
  public Dictionary<string, BaseMappingDescriptor> ParseCustomMappings(
    SyntaxNode classNode,
    MapperMethodMetadata methodMetadata,
    CancellationToken ct)
  {
    var customMappings = new Dictionary<string, BaseMappingDescriptor>();

    if (classNode is not ClassDeclarationSyntax classDeclaration)
    {
      return customMappings;
    }

    // Find the constructor
    var constructor = classDeclaration.Members
      .OfType<ConstructorDeclarationSyntax>()
      .FirstOrDefault();

    if (constructor?.Body == null)
    {
      return customMappings;
    }

    // Find all MapMember invocations
    var mapMemberCalls = constructor.Body.Statements
      .OfType<ExpressionStatementSyntax>()
      .Select(es => es.Expression)
      .OfType<InvocationExpressionSyntax>()
      .Where(inv =>
        inv.Expression is IdentifierNameSyntax { Identifier.Text: Constants.MapMemberMethodName });

    foreach (var mapMemberCall in mapMemberCalls)
    {
      if (ct.IsCancellationRequested)
      {
        break;
      }

      // E.G. MapMember(carDto => carDto.CarId, car => car.Id)
      if (mapMemberCall.ArgumentList.Arguments.Count != 2)
      {
        continue;
      }

      // First argument: destination member (e.g., carDto => carDto.CarId)
      var destArg = mapMemberCall.ArgumentList.Arguments[0].Expression;
      var destPropertyName = ExtractDestinationPropertyName(destArg);

      if (destPropertyName is null or "")
      {
        continue;
      }

      // Second argument: source expression (e.g., car => car.Id or car => car.Name + "Model")
      var sourceArg = mapMemberCall.ArgumentList.Arguments[1].Expression;

      // Check if it's a lambda block (not supported)
      if (sourceArg is SimpleLambdaExpressionSyntax { Block: not null }
          or ParenthesizedLambdaExpressionSyntax { Block: not null })
      {
        var diagnostic = MapperDiagnostic.LambdaBlockNotSupported(
          mapMemberCall.GetLocation(),
          destPropertyName,
          Constants.MapMemberMethodName);
        methodMetadata.AddDiagnostic(diagnostic);

        // Add a diagnosed property descriptor to prevent "missing property" diagnostic
        var diagnosedProperty = new DiagnosedPropertyDescriptor(destPropertyName);
        customMappings[destPropertyName] = diagnosedProperty;
        continue;
      }

      var sourceExpression = ExtractSourceExpression(sourceArg, methodMetadata);

      if (sourceExpression is null or "")
      {
        continue;
      }

      var mapping = new MappingDescriptor(destPropertyName, sourceExpression);
      customMappings[destPropertyName] = mapping;
    }

    return customMappings;
  }


  private static string? ExtractDestinationPropertyName(ExpressionSyntax expression)
  {
    // Handle lambda expressions: x => x.Property
    if (expression is SimpleLambdaExpressionSyntax { ExpressionBody: MemberAccessExpressionSyntax memberAccess })
    {
      return memberAccess.Name.Identifier.Text;
    }

    return null;
  }

  private static string? ExtractSourceExpression(
    ExpressionSyntax expression,
    MapperMethodMetadata methodMetadata)
  {
    switch (expression)
    {
      // Handle lambda expressions with expression body: src => src.Id or src => src.Name + "Model"
      case SimpleLambdaExpressionSyntax { ExpressionBody: not null } lambdaWithExpression:
      {
        var lambdaParameterName = lambdaWithExpression.Parameter.Identifier.Text;
        var actualParameterName = methodMetadata.SourceObjectParameter.Name;
        var sourceExpression = lambdaWithExpression.ExpressionBody.ToString();

        // Replace lambda parameter name with actual method parameter name
        // This handles cases like: src => src.Owner.ToDto() becomes car.Owner.ToDto()
        return ParameterNameReplacer.ReplaceParameterName(sourceExpression, lambdaParameterName, actualParameterName);
      }
      // Handle parenthesized lambda expressions with multiple parameters: (car, driver) => driver.ToDto()
      case ParenthesizedLambdaExpressionSyntax { ExpressionBody: not null } parenthesizedLambda:
      {
        var sourceExpression = parenthesizedLambda.ExpressionBody.ToString();

        // Map lambda parameter names to actual method parameter names
        for (var i = 0;
             i < parenthesizedLambda.ParameterList.Parameters.Count && i < methodMetadata.Parameters.Count;
             i++)
        {
          var lambdaParameterName = parenthesizedLambda.ParameterList.Parameters[i].Identifier.Text;
          var actualParameterName = methodMetadata.Parameters[i].Name;
          sourceExpression = ParameterNameReplacer.ReplaceParameterName(sourceExpression, lambdaParameterName, actualParameterName);
        }

        return sourceExpression;
      }
      // Handle method group with member access: CarNameBuilder.GetCarName
      case MemberAccessExpressionSyntax memberAccessSyntax:
        // Convert method group to method invocation: CarNameBuilder.GetCarName(car)
        // Note: Method groups only pass the first parameter by default
        return $"{memberAccessSyntax}({methodMetadata.SourceObjectParameter.Name})";
      // Handle method group: GetCarName
      case IdentifierNameSyntax identifierNameSyntax:
        // Convert method group to method invocation: GetCarName(car)
        // Note: Method groups only pass the first parameter by default
        return $"{identifierNameSyntax.Identifier.Text}({methodMetadata.SourceObjectParameter.Name})";
      default:
        return null;
    }
  }
}
