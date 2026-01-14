using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using Mapgen.Analyzer.Abstractions;
using Mapgen.Analyzer.Mapper.Diagnostics;
using Mapgen.Analyzer.Mapper.Metadata;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Mapgen.Analyzer.Mapper
{
  [Generator]
  public class MappingGenerator : IIncrementalGenerator
  {
    private readonly MapperDiagnosticsReporter _diagnosticReporter = new();

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
      var provider = context.SyntaxProvider.ForAttributeWithMetadataName(
        MapgenProject.MapperAttributeFullName,
        Predicate,
        Transform
      );

      context.RegisterSourceOutput(provider, Generate);
    }

    private void Generate(SourceProductionContext ctx, MappingConfigurationMetadata? configMetadata)
    {
      if (configMetadata is null)
      {
        return;
      }

      // Report all diagnostics (lambda blocks, unmapped properties, etc.)
      _diagnosticReporter.Report(ctx, configMetadata);

      var templateEngine = new MapperTemplateEngine(configMetadata);
      var classSource = templateEngine.GenerateMapperClass();
      ctx.AddSource(templateEngine.MapperConfigFileName, SourceText.From(classSource, Encoding.UTF8));
    }

    private MappingConfigurationMetadata? Transform(GeneratorAttributeSyntaxContext ctx, CancellationToken ct)
    {
      if (ctx.TargetSymbol is not INamedTypeSymbol mapperDeclaration)
      {
        return null;
      }

      var mapperNamespace = mapperDeclaration.ContainingNamespace.ToDisplayString();
      var mapperName = mapperDeclaration.Name;

      // Extract using directives from the source file
      var usings = ExtractUsings(ctx.TargetNode);

      var classDiagnostics = TryTransformMappingMethod(ctx, ct, mapperDeclaration, mapperName, out var methodMetadata);

      return new MappingConfigurationMetadata(usings, mapperNamespace, mapperName, mapperDeclaration.DeclaredAccessibility, methodMetadata, classDiagnostics);
    }

    private IReadOnlyList<string> ExtractUsings(SyntaxNode node)
    {
      var usings = new List<string>();

      // Navigate up to find the compilation unit (root of the syntax tree)
      var compilationUnit = node.AncestorsAndSelf()
        .OfType<CompilationUnitSyntax>()
        .FirstOrDefault();

      if (compilationUnit != null)
      {
        foreach (var usingDirective in compilationUnit.Usings)
        {
          // Check if this is an alias (e.g., using CarConstants = Namespace.Type)
          if (usingDirective.Alias is not null && usingDirective.Name is not null)
          {
            var aliasName = usingDirective.Alias.Name.ToString();
            var targetName = usingDirective.Name.ToString();
            usings.Add($"{aliasName} = {targetName}");
            continue;
          }

          if (usingDirective.Name is not null)
          {
            // Regular using directive
            var usingName = usingDirective.Name.ToString();
            usings.Add(usingName);
          }
        }
      }

      return usings;
    }

    private static List<MapperDiagnostic> TryTransformMappingMethod(
      GeneratorAttributeSyntaxContext ctx,
      CancellationToken ct,
      INamedTypeSymbol mapperDeclaration,
      string mapperName,
      out MapperMethodMetadata? methodMetadata)
    {
      var classDiagnostics = new List<MapperDiagnostic>();
      var partialMethods = mapperDeclaration.GetMembers()
        .OfType<IMethodSymbol>()
        .Where(m => m.IsPartialDefinition)
        .ToList();

      // Check if there are multiple partial methods
      if (partialMethods.Count > 1)
      {
        // Report error for all methods after the first one
        for (var i = 1; i < partialMethods.Count; i++)
        {
          var method = partialMethods[i];
          var diagnostic = MapperDiagnostic.MultipleMappingMethods(
            method.Locations.FirstOrDefault(),
            mapperName,
            method.Name);
          classDiagnostics.Add(diagnostic);
        }
      }

      // Only process the first partial method
      methodMetadata = null;
      if (partialMethods.Count > 0 && !ct.IsCancellationRequested)
      {
        var firstMethod = partialMethods[0];
        var methodTransformer = new MapperMethodTransformer(ctx.SemanticModel);
        methodMetadata = methodTransformer.Transform(firstMethod, ctx.TargetNode, ct);
      }

      return classDiagnostics;
    }

    private bool Predicate(SyntaxNode node, CancellationToken ct) => true;
  }
}
