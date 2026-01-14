using System.Collections.Generic;
using System.Linq;
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

      if (!method.IsPartialDefinition)
      {
        continue;
      }

      // Extract method info
      var accessibility = method.DeclaredAccessibility;
      var methodName = method.Name;
      var returnTypeSymbol = method.ReturnType;
      var parameters = method.Parameters.Select(p => new ParameterInfo(p.Name, p.Type)).ToList();

      if (parameters.Count >= 1)
      {
        var extensionParameter = parameters[0];
        var additionalParameters = parameters.Skip(1).ToList();
        extensionMethods.Add(new ExtensionMethodInfo(accessibility, methodName, returnTypeSymbol, extensionParameter, additionalParameters));
      }
    }

    if (extensionMethods.Count == 0)
    {
      return null;
    }

    // Extract using directives from the source file
    var usings = SyntaxHelpers.ExtractUsings(ctx.TargetNode);

    return new MapperExtensionsMetadata(usings, mapperNamespace, mapperClassAccessibility, mapperClassName, extensionMethods);
  }

  private bool Predicate(SyntaxNode node, CancellationToken ct) => true;
}
