using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Mapgen.Analyzer.Mapper.MappingDescriptors;
using Mapgen.Analyzer.Mapper.Metadata;
using Mapgen.Analyzer.Mapper.Utils;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mapgen.Analyzer.Mapper.Strategies;

public sealed class CollectionMappingStrategy : BaseMappingStrategy
{
  public Dictionary<string, BaseMappingDescriptor> ParseCollectionMappings(
    SyntaxNode classNode,
    MapperMethodMetadata methodMetadata,
    CancellationToken ct)
  {
    var collectionMappings = new Dictionary<string, BaseMappingDescriptor>();

    var constructor = GetMapperConstructor(classNode);
    if (constructor is null)
    {
      return collectionMappings;
    }

    // Find all MapCollection invocations (both generic and non-generic)
    var mapCollectionCalls = FindMethodInvocations(constructor, Constants.MapCollectionMethodName);

    foreach (var mapCollectionCall in mapCollectionCalls)
    {
      if (ct.IsCancellationRequested)
      {
        break;
      }

      // MapCollection can have 2 or 3 arguments:
      // - 2 args: MapCollection(dto => dto.Cars, car => car.ToDto(driver, garage))
      // - 3 args: MapCollection(dto => dto.CarsDto, garage => garage.Cars, (car, garage, driver) => car.ToDto(driver))
      var argCount = mapCollectionCall.ArgumentList.Arguments.Count;
      if (argCount != 2 && argCount != 3)
      {
        continue;
      }

      // First argument: destination collection (e.g., dto => dto.Cars)
      var destArg = mapCollectionCall.ArgumentList.Arguments[0].Expression;
      var destProperty = ExtractPropertyFromExpression(destArg, methodMetadata);

      if (destProperty is null)
      {
        continue;
      }

      // Determine source property and item transformation based on argument count
      ExpressionSyntax? sourcePropertyArg = null;
      ExpressionSyntax itemTransformArg;

      if (argCount == 3)
      {
        // 3 args: dest prop, source prop, item transform
        sourcePropertyArg = mapCollectionCall.ArgumentList.Arguments[1].Expression;
        itemTransformArg = mapCollectionCall.ArgumentList.Arguments[2].Expression;
      }
      else
      {
        // 2 args: dest prop, item transform
        itemTransformArg = mapCollectionCall.ArgumentList.Arguments[1].Expression;
      }

      // Validate lambda expression body (blocks not supported)
      if (!ValidateLambdaExpressionBody(
        itemTransformArg,
        destProperty.Name,
        Constants.MapCollectionMethodName,
        mapCollectionCall.GetLocation(),
        methodMetadata))
      {
        collectionMappings[destProperty.Name] = new DiagnosedPropertyDescriptor(destProperty.Name);
        continue;
      }

      // Extract the collection property name and item transformation
      var collectionMapping = ExtractCollectionMapping(itemTransformArg, methodMetadata, destProperty, sourcePropertyArg);

      if (collectionMapping is null or "")
      {
        continue;
      }

      var mapping = new MappingDescriptor(destProperty.Name, collectionMapping);
      collectionMappings[destProperty.Name] = mapping;
    }

    return collectionMappings;
  }


  private static IPropertySymbol? ExtractPropertyFromExpression(ExpressionSyntax expression,
    MapperMethodMetadata methodMetadata)
  {
    // Handle lambda expressions: x => x.Property
    if (expression is not SimpleLambdaExpressionSyntax { ExpressionBody: MemberAccessExpressionSyntax memberAccess })
    {
      return null;
    }

    var propertyName = memberAccess.Name.Identifier.Text;
    return methodMetadata.ReturnType.GetAllProperties()
      .FirstOrDefault(p => p.Name == propertyName);
  }

  private static string? ExtractCollectionMapping(
    ExpressionSyntax itemTransformExpression,
    MapperMethodMetadata methodMetadata,
    IPropertySymbol destProperty,
    ExpressionSyntax? sourcePropertyExpression = null)
  {
    // Determine the source collection access
    string sourceCollectionAccess;

    if (sourcePropertyExpression != null)
    {
      // Explicit source property provided: extract it from the lambda expression
      // Supports:
      // - Simple member access: src => src.Members
      // - Method chains: src => src.Members.Where(...), src => src.Members.OrderBy(...)
      string? sourceParamName = null;
      ExpressionSyntax? expressionBody = null;

      switch (sourcePropertyExpression)
      {
        case SimpleLambdaExpressionSyntax { ExpressionBody: not null } simpleLambda:
          sourceParamName = simpleLambda.Parameter.Identifier.Text;
          expressionBody = simpleLambda.ExpressionBody;
          break;
        case ParenthesizedLambdaExpressionSyntax { ExpressionBody: not null, ParameterList.Parameters.Count: > 0 } parenLambda:
          sourceParamName = parenLambda.ParameterList.Parameters[0].Identifier.Text;
          expressionBody = parenLambda.ExpressionBody;
          break;
      }

      if (expressionBody != null && sourceParamName != null)
      {
        // Find the parameter in the method metadata by name
        var sourceParameter = methodMetadata.Parameters.FirstOrDefault(p => p.Name == sourceParamName) ?? methodMetadata.SourceObjectParameter;

        // Replace the lambda parameter name with the actual method parameter name in the expression
        var expressionText = expressionBody.ToString();

        // Replace only whole word matches of the parameter name
        // e.g., "src.Members.Where(m => ...)" -> "source.Members.Where(m => ...)"
        sourceCollectionAccess = ParameterNameReplacer.ReplaceParameterName(expressionText, sourceParamName, sourceParameter.Name);
      }
      else
      {
        return null; // Unsupported source property expression
      }
    }
    else
    {
      // No explicit source property: infer from destination property name
      var sourceProperty = (methodMetadata.SourceObjectParameter.Symbol.Type as INamedTypeSymbol)
        ?.GetAllProperties()
        .FirstOrDefault(p => p.Name == destProperty.Name);

      if (sourceProperty is null)
      {
        return null; // Source property not found
      }

      sourceCollectionAccess = $"{methodMetadata.SourceObjectParameter.Name}.{sourceProperty.Name}";
    }

    // Handle lambda expressions: car => car.ToDto(driver, garage)
    if (itemTransformExpression is SimpleLambdaExpressionSyntax { ExpressionBody: not null } lambda)
    {
      var itemParameterName = lambda.Parameter.Identifier.Text;
      var itemTransformation = lambda.ExpressionBody.ToString();

      return CollectionHelpers.BuildCollectionMappingExpression(
        sourceCollectionAccess,
        itemParameterName,
        itemTransformation,
        destProperty.Type);
    }

    // Handle parenthesized lambda: (car, garage, driver) => car.ToDto(driver, garage)
    // Only use the first parameter (the collection item) in Select, others are captured from closure
    if (itemTransformExpression is ParenthesizedLambdaExpressionSyntax
      {
        ExpressionBody: not null
      } parenthesizedLambda)
    {
      if (parenthesizedLambda.ParameterList.Parameters.Count == 0)
      {
        return null;
      }

      // Only use the first parameter (the collection item)
      var itemParameterName = parenthesizedLambda.ParameterList.Parameters[0].Identifier.Text;

      // Replace lambda parameter names with actual method parameter names (skip first - it's the item)
      var itemTransformation = LambdaParameterReplacer.ExtractAndReplaceParameters(
        parenthesizedLambda,
        methodMetadata,
        skipFirstLambdaParams: 1); // Skip first lambda param (collection item)

      if (itemTransformation == null)
      {
        return null;
      }

      return CollectionHelpers.BuildCollectionMappingExpression(
        sourceCollectionAccess,
        itemParameterName,
        itemTransformation,
        destProperty.Type);
    }

    return null;
  }
}
