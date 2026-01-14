using System.Collections.Generic;

using Microsoft.CodeAnalysis;

namespace Mapgen.Analyzer.Extensions
{
  public sealed class ExtensionMethodInfo
  {
    public Accessibility Accessibility { get; private set; }
    public string MethodName { get; }
    public ITypeSymbol ReturnTypeSymbol { get; }
    public ParameterInfo ExtensionParameter { get; }
    public List<ParameterInfo> AdditionalParameters { get; }

    public ExtensionMethodInfo(Accessibility accessibility, string methodName, ITypeSymbol returnTypeSymbol, ParameterInfo extensionParameter, List<ParameterInfo> additionalParameters)
    {
      Accessibility = accessibility;
      MethodName = methodName;
      ReturnTypeSymbol = returnTypeSymbol;
      ExtensionParameter = extensionParameter;
      AdditionalParameters = additionalParameters;
    }
  }
}
