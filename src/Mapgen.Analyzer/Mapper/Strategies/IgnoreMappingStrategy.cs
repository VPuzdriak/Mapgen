using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Mapgen.Analyzer.Mapper.MappingDescriptors;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mapgen.Analyzer.Mapper.Strategies;

public sealed class IgnoreMappingStrategy
{
  public Dictionary<string, IgnoredPropertyDescriptor> ParseIgnoreMappings(SyntaxNode classNode, CancellationToken ct)
  {
    var ignoreMappings = new Dictionary<string, IgnoredPropertyDescriptor>();

    if (classNode is not ClassDeclarationSyntax classDeclaration)
    {
      return ignoreMappings;
    }

    // Find the constructor
    var constructor = classDeclaration.Members
      .OfType<ConstructorDeclarationSyntax>()
      .FirstOrDefault();

    if (constructor?.Body == null)
    {
      return ignoreMappings;
    }

    // Find all IgnoreMember invocations
    var ignoreMemberCalls = constructor.Body.Statements
      .OfType<ExpressionStatementSyntax>()
      .Select(es => es.Expression)
      .OfType<InvocationExpressionSyntax>()
      .Where(inv =>
        inv.Expression is IdentifierNameSyntax { Identifier.Text: Constants.IgnoreMemberMethodName });

    foreach (var ignoreMemberCall in ignoreMemberCalls)
    {
      if (ct.IsCancellationRequested)
      {
        break;
      }

      // E.G. IgnoreMember(carDto => carDto.CarId)
      if (ignoreMemberCall.ArgumentList.Arguments.Count != 1)
      {
        continue;
      }

      // First argument: destination member (e.g., carDto => carDto.CarId)
      var destArg = ignoreMemberCall.ArgumentList.Arguments[0].Expression;
      var destMemberName = ExtractDestinationPropertyName(destArg);

      if (destMemberName is null || string.IsNullOrEmpty(destMemberName))
      {
        continue;
      }

      var mapping = new IgnoredPropertyDescriptor(destMemberName, ignoreMemberCall.GetLocation());
      ignoreMappings[destMemberName] = mapping;
    }

    return ignoreMappings;
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
}
