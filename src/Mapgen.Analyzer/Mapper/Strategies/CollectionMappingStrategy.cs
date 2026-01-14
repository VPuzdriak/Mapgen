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

public sealed class CollectionMappingStrategy
{
  public Dictionary<string, BaseMappingDescriptor> ParseCollectionMappings(
    SyntaxNode classNode,
    MapperMethodMetadata methodMetadata,
    CancellationToken ct)
  {
    var collectionMappings = new Dictionary<string, BaseMappingDescriptor>();

    if (classNode is not ClassDeclarationSyntax classDeclaration)
    {
      return collectionMappings;
    }

    // Find the constructor
    var constructor = classDeclaration.Members
      .OfType<ConstructorDeclarationSyntax>()
      .FirstOrDefault();

    if (constructor?.Body == null)
    {
      return collectionMappings;
    }

    // Find all MapCollection invocations (both generic and non-generic)
    var mapCollectionCalls = constructor.Body.Statements
      .OfType<ExpressionStatementSyntax>()
      .Select(es => es.Expression)
      .OfType<InvocationExpressionSyntax>()
      .Where(IsMapCollectionInvocation);

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

      // Check if it's a lambda block (not supported)
      if (itemTransformArg is SimpleLambdaExpressionSyntax { Block: not null }
          or ParenthesizedLambdaExpressionSyntax { Block: not null })
      {
        var diagnostic = MapperDiagnostic.LambdaBlockNotSupported(
          mapCollectionCall.GetLocation(),
          destProperty.Name,
          Constants.MapCollectionMethodName);
        methodMetadata.AddDiagnostic(diagnostic);

        var diagnosedProperty = new DiagnosedPropertyDescriptor(destProperty.Name);
        collectionMappings[destProperty.Name] = diagnosedProperty;
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

  private static bool IsMapCollectionInvocation(InvocationExpressionSyntax invocation)
  {
    return invocation.Expression switch
    {
      // Non-generic: MapCollection(...)
      IdentifierNameSyntax { Identifier.Text: Constants.MapCollectionMethodName } => true,
      // Generic: MapCollection<T1, T2>(...)
      GenericNameSyntax { Identifier.Text: Constants.MapCollectionMethodName } => true,
      _ => false
    };
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
    return methodMetadata.ReturnType.GetMembers()
      .OfType<IPropertySymbol>()
      .FirstOrDefault(p => p.Name == propertyName);
  }

  private static string? ExtractCollectionMapping(
    ExpressionSyntax itemTransformExpression,
    MapperMethodMetadata methodMetadata,
    IPropertySymbol destProperty,
    ExpressionSyntax? sourcePropertyExpression = null)
  {
    // Determine the target collection type conversion method (based on destination type)
    var conversionMethod = GetCollectionConversionMethod(destProperty.Type);

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

      if (sourcePropertyExpression is SimpleLambdaExpressionSyntax { ExpressionBody: not null } simpleLambda)
      {
        sourceParamName = simpleLambda.Parameter.Identifier.Text;
        expressionBody = simpleLambda.ExpressionBody;
      }
      else if (sourcePropertyExpression is ParenthesizedLambdaExpressionSyntax { ExpressionBody: not null } parenLambda)
      {
        if (parenLambda.ParameterList.Parameters.Count > 0)
        {
          sourceParamName = parenLambda.ParameterList.Parameters[0].Identifier.Text;
          expressionBody = parenLambda.ExpressionBody;
        }
      }

      if (expressionBody != null && sourceParamName != null)
      {
        // Find the parameter in the method metadata by name
        var sourceParameter = methodMetadata.Parameters.FirstOrDefault(p => p.Name == sourceParamName) ?? methodMetadata.SourceObjectParameter;

        // Replace the lambda parameter name with the actual method parameter name in the expression
        var expressionText = expressionBody.ToString();
        
        // Replace only whole word matches of the parameter name
        // e.g., "src.Members.Where(m => ...)" -> "source.Members.Where(m => ...)"
        sourceCollectionAccess = ReplaceParameterInExpression(expressionText, sourceParamName, sourceParameter.Name);
      }
      else
      {
        return null; // Unsupported source property expression
      }
    }
    else
    {
      // No explicit source property: infer from destination property name
      var sourceProperty = methodMetadata.SourceObjectParameter.Symbol.Type.GetMembers()
        .OfType<IPropertySymbol>()
        .FirstOrDefault(p => p.Name == destProperty.Name);

      if (sourceProperty == null)
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

      // Build the full Select expression with appropriate conversion
      var selectExpression =
        $"{sourceCollectionAccess}.Select({itemParameterName} => {itemTransformation}).{conversionMethod}()";

      return selectExpression;
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
      var itemTransformation = parenthesizedLambda.ExpressionBody.ToString();

      // Replace lambda parameter names with actual method parameter names
      // Lambda params after the first one (collection item) map to method parameters
      for (var i = 1; i < parenthesizedLambda.ParameterList.Parameters.Count; i++)
      {
        var lambdaParamName = parenthesizedLambda.ParameterList.Parameters[i].Identifier.Text;
        var methodParamIndex = i - 1; // Skip the first lambda param (collection item)

        if (methodParamIndex < methodMetadata.Parameters.Count)
        {
          var methodParamName = methodMetadata.Parameters[methodParamIndex].Name;
          itemTransformation = ParameterNameReplacer.ReplaceParameterName(
            itemTransformation,
            lambdaParamName,
            methodParamName
          );
        }
      }

      // Build the full Select expression with appropriate conversion
      var selectExpression =
        $"{sourceCollectionAccess}.Select({itemParameterName} => {itemTransformation}).{conversionMethod}()";

      return selectExpression;
    }

    return null;
  }

  private static string GetCollectionConversionMethod(ITypeSymbol destinationType)
  {
    // If destination is an array, use ToArray()
    if (destinationType is IArrayTypeSymbol)
    {
      return "ToArray";
    }

    // If destination is a generic collection type, determine the appropriate method
    if (destinationType is INamedTypeSymbol namedType)
    {
      var typeDefinition = namedType.OriginalDefinition;
      var typeName = typeDefinition.ToDisplayString();

      // For HashSet<T>, use ToHashSet()
      if (typeName.StartsWith("System.Collections.Generic.HashSet<"))
      {
        return "ToHashSet";
      }

      // For immutable collections, use the appropriate ToImmutableXxx() method
      if (typeName.StartsWith("System.Collections.Immutable.ImmutableArray<"))
      {
        return "ToImmutableArray";
      }

      if (typeName.StartsWith("System.Collections.Immutable.IImmutableList<") ||
          typeName.StartsWith("System.Collections.Immutable.ImmutableList<"))
      {
        return "ToImmutableList";
      }

      if (typeName.StartsWith("System.Collections.Immutable.IImmutableSet<") ||
          typeName.StartsWith("System.Collections.Immutable.ImmutableHashSet<"))
      {
        return "ToImmutableHashSet";
      }

      // For List<T>, IList<T>, ICollection<T>, IEnumerable<T>, IReadOnlyList<T>, IReadOnlyCollection<T>, use ToList()
      if (typeName.StartsWith("System.Collections.Generic.List<") ||
          typeName.StartsWith("System.Collections.Generic.IList<") ||
          typeName.StartsWith("System.Collections.Generic.ICollection<") ||
          typeName.StartsWith("System.Collections.Generic.IReadOnlyList<") ||
          typeName.StartsWith("System.Collections.Generic.IReadOnlyCollection<") ||
          typeName.StartsWith("System.Collections.Generic.IEnumerable<"))
      {
        return "ToList";
      }
    }

    // Default to ToList()
    return "ToList";
  }

  private static string ReplaceParameterInExpression(string expression, string oldParamName, string newParamName)
  {
    // Replace the lambda parameter name with the actual method parameter name
    // This needs to be careful to only replace the outer lambda parameter, not nested ones
    // e.g., "src.Members.Where(m => m.Name.StartsWith("N"))" 
    //   -> "source.Members.Where(m => m.Name.StartsWith("N"))"
    // We only want to replace "src" with "source", not "m"
    
    // Use ParameterNameReplacer utility if it exists, otherwise do simple replacement
    return ParameterNameReplacer.ReplaceParameterName(expression, oldParamName, newParamName);
  }
}
