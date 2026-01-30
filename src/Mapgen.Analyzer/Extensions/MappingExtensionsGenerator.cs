using System.Collections.Generic;
using System.Text;
using System.Threading;

using Mapgen.Analyzer.Mapper.Utils;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Mapgen.Analyzer.Extensions;

[Generator]
public class MappingExtensionsGenerator : IIncrementalGenerator
{
  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    var provider = context.SyntaxProvider.ForAttributeWithMetadataName(
      MapgenProject.MapperAttributeFullName,
      Predicate,
      Transform
    );

    context.RegisterSourceOutput(provider, Generate);
  }

  private void Generate(SourceProductionContext ctx, MapperExtensionsMetadata? metadata)
  {
    if (metadata is null)
    {
      return;
    }

    var templateEngine = new MappingExtensionsTemplateEngine(metadata);
    var classSource = templateEngine.GenerateExtensionsClass();
    ctx.AddSource($"{metadata.MapperClassName}Extensions.g.cs", SourceText.From(classSource, Encoding.UTF8));
  }


  private MapperExtensionsMetadata? Transform(GeneratorAttributeSyntaxContext ctx, CancellationToken ct)
  {
    if (ctx.TargetSymbol is not INamedTypeSymbol mapperClass)
    {
      return null;
    }

    var mapperNamespace = mapperClass.ContainingNamespace.ToDisplayString();
    var mapperClassAccessibility = mapperClass.DeclaredAccessibility;
    var mapperClassName = mapperClass.Name;

    var extensionMethods = new List<ExtensionMethodInfo>();

    foreach (var method in mapperClass.GetMembers().OfType<IMethodSymbol>())
    {
      if (ct.IsCancellationRequested)
      {
        break;
      }

      if (!TryCreateMethodInfo(method, out ExtensionMethodInfo? methodInfo) || methodInfo is null)
      {
        continue;
      }

      extensionMethods.Add(methodInfo);
    }

    if (extensionMethods.Count == 0)
    {
      return null;
    }

    // Extract using directives from the source file
    var usings = SyntaxHelpers.ExtractUsings(ctx.TargetNode);
    return new MapperExtensionsMetadata(usings, mapperNamespace, mapperClassAccessibility, mapperClassName, extensionMethods);
  }

  private bool TryCreateMethodInfo(IMethodSymbol method, out ExtensionMethodInfo? methodInfo)
  {
    if (!method.IsPartialDefinition)
    {
      methodInfo = null;
      return false;
    }

    var methodSyntax = SyntaxHelpers.FindMethodDeclaration(method);

    if (methodSyntax is null)
    {
      methodInfo = null;
      return false;
    }

    if (method.Parameters.Length == 0)
    {
      methodInfo = null;
      return false;
    }

    methodInfo = new ExtensionMethodInfo(method, methodSyntax);
    return true;
  }

  private bool Predicate(SyntaxNode node, CancellationToken ct) => true;
}
