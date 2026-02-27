using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Mapgen.Analyzer.Mapper.Metadata;
using Mapgen.Analyzer.Mapper.Utils;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mapgen.Analyzer.Mapper;

public class MappingParser
{
  private readonly SemanticModel _semanticModel;

  public MappingParser(SemanticModel semanticModel)
  {
    _semanticModel = semanticModel;
  }

  public List<IncludedMapperInfo> ParseIncludedMappers(
    SyntaxNode classNode,
    CancellationToken ct)
  {
    var includedMappers = new List<IncludedMapperInfo>();

    if (classNode is not ClassDeclarationSyntax classDeclaration)
    {
      return includedMappers;
    }

    var constructor = SyntaxHelpers.FindConstructor(classDeclaration);
    if (constructor?.Body is null)
    {
      return includedMappers;
    }

    // Find all IncludeMappers invocations
    var includeMappersCall = constructor.Body.Statements
      .OfType<ExpressionStatementSyntax>()
      .Select(es => es.Expression)
      .OfType<InvocationExpressionSyntax>()
      .FirstOrDefault(inv =>
        inv.Expression is IdentifierNameSyntax { Identifier.Text: MappingConfigurationMethods.IncludeMappersMethodName });

    if (includeMappersCall is null)
    {
      return includedMappers;
    }

    // Get the argument (should be a collection expression)
    if (includeMappersCall.ArgumentList.Arguments.Count != 1)
    {
      return includedMappers;
    }

    var argument = includeMappersCall.ArgumentList.Arguments[0].Expression;

    // Handle collection expression: [new CarMapper(), new DriverMapper()]
    if (argument is CollectionExpressionSyntax collectionExpression)
    {
      foreach (var element in collectionExpression.Elements)
      {
        if (ct.IsCancellationRequested)
        {
          break;
        }

        // Each element should be an ExpressionElementSyntax containing an ObjectCreationExpressionSyntax
        if (element is ExpressionElementSyntax { Expression: ObjectCreationExpressionSyntax objectCreation })
        {
          TryAddMapperFromCreationExpression(objectCreation, includedMappers, ct);
        }
      }
    }
    // Handle array creation expressions: new object[] { ... }
    else if (argument is ArrayCreationExpressionSyntax arrayCreation)
    {
      ExtractMappersFromInitializer(arrayCreation.Initializer, includedMappers, ct);
    }
    // Handle implicit array creation: new[] { ... }
    else if (argument is ImplicitArrayCreationExpressionSyntax implicitArrayCreation)
    {
      ExtractMappersFromInitializer(implicitArrayCreation.Initializer, includedMappers, ct);
    }

    return includedMappers;
  }

  public List<EnumMappingDeclaration> ParseMapEnumDeclarations(
    SyntaxNode classNode,
    CancellationToken ct)
  {
    var enumMappings = new List<EnumMappingDeclaration>();

    if (classNode is not ClassDeclarationSyntax classDeclaration)
    {
      return enumMappings;
    }

    var constructor = SyntaxHelpers.FindConstructor(classDeclaration);
    if (constructor?.Body is null)
    {
      return enumMappings;
    }

    // Find all MapEnum invocations
    var mapEnumCalls = constructor.Body.Statements
      .OfType<ExpressionStatementSyntax>()
      .Select(es => es.Expression)
      .OfType<InvocationExpressionSyntax>()
      .Where(inv =>
        inv.Expression is GenericNameSyntax { Identifier.Text: MappingConfigurationMethods.MapEnumMethodName });

    foreach (var mapEnumCall in mapEnumCalls)
    {
      if (ct.IsCancellationRequested)
      {
        break;
      }

      if (mapEnumCall.Expression is not GenericNameSyntax genericName)
      {
        continue;
      }

      // MapEnum should have exactly 2 type arguments: <TSource, TDest>
      if (genericName.TypeArgumentList.Arguments.Count != 2)
      {
        continue;
      }

      var sourceTypeInfo = _semanticModel.GetTypeInfo(genericName.TypeArgumentList.Arguments[0], ct);
      var destTypeInfo = _semanticModel.GetTypeInfo(genericName.TypeArgumentList.Arguments[1], ct);

      if (sourceTypeInfo.Type is not null && destTypeInfo.Type is not null)
      {
        var location = mapEnumCall.GetLocation();
        enumMappings.Add(new EnumMappingDeclaration(sourceTypeInfo.Type, destTypeInfo.Type, location));
      }
    }

    return enumMappings;
  }

  private void ExtractMappersFromInitializer(
    InitializerExpressionSyntax? initializer,
    List<IncludedMapperInfo> includedMappers,
    CancellationToken ct)
  {
    if (initializer is null)
    {
      return;
    }

    foreach (var expression in initializer.Expressions)
    {
      if (ct.IsCancellationRequested)
      {
        break;
      }

      if (expression is ObjectCreationExpressionSyntax objectCreation)
      {
        TryAddMapperFromCreationExpression(objectCreation, includedMappers, ct);
      }
    }
  }

  private void TryAddMapperFromCreationExpression(
    ObjectCreationExpressionSyntax objectCreation,
    List<IncludedMapperInfo> includedMappers,
    CancellationToken ct)
  {
    var typeInfo = _semanticModel.GetTypeInfo(objectCreation, ct);
    if (typeInfo.Type is INamedTypeSymbol mapperType)
    {
      includedMappers.Add(new IncludedMapperInfo(mapperType));
    }
  }
}
