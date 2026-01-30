using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mapgen.Analyzer.Extensions
{
  public sealed class ExtensionMethodInfo
  {
    private readonly List<ParameterInfo> _additionalParameters = [];
    public IMethodSymbol MethodSymbol { get; }
    public string ReturnTypeSyntax { get; }
    public Accessibility Accessibility => MethodSymbol.DeclaredAccessibility;
    public string MethodName => MethodSymbol.Name;
    public ParameterInfo ExtensionParameter { get; }
    public IReadOnlyList<ParameterInfo> AdditionalParameters => _additionalParameters;

    public ExtensionMethodInfo(IMethodSymbol methodSymbol, MethodDeclarationSyntax methodDeclarationSyntax)
    {
      MethodSymbol = methodSymbol;
      ReturnTypeSyntax = methodDeclarationSyntax.ReturnType.ToString();

      var extensionParamSymbol = methodSymbol.Parameters[0];
      ExtensionParameter = new ParameterInfo(extensionParamSymbol, methodDeclarationSyntax.ParameterList.Parameters[0].Type!.ToString());

      for (int i = 1; i < methodSymbol.Parameters.Length; i++)
      {
        var additionalParamSymbol = methodSymbol.Parameters[i];
        _additionalParameters.Add(new ParameterInfo(additionalParamSymbol, methodDeclarationSyntax.ParameterList.Parameters[i].Type!.ToString()));
      }
    }
  }
}
