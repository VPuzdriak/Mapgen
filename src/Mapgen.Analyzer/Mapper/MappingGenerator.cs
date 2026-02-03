using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using Mapgen.Analyzer.Mapper.Diagnostics;
using Mapgen.Analyzer.Mapper.Metadata;
using Mapgen.Analyzer.Mapper.Utils;

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
      var usings = SyntaxHelpers.ExtractUsings(ctx.TargetNode);

      var classDiagnostics = new List<MapperDiagnostic>();
      // Validate mapper constructor has no parameters
      ValidateMapperConstructor(ctx.TargetNode, mapperName, classDiagnostics);

      
      var methodMetadata = TransformMappingMethod(ctx, mapperDeclaration, mapperName, classDiagnostics, ct);
      return new MappingConfigurationMetadata(usings, mapperNamespace, mapperName, mapperDeclaration.DeclaredAccessibility, methodMetadata, classDiagnostics);
    }

    private static void ValidateMapperConstructor(SyntaxNode classNode, string mapperName, List<MapperDiagnostic> classDiagnostics)
    {
      if (classNode is not ClassDeclarationSyntax classDeclaration)
      {
        return;
      }

      var constructor = SyntaxHelpers.FindConstructor(classDeclaration);

      // If no constructor, it's fine (will use default parameterless constructor)
      if (constructor is null)
      {
        return;
      }

      // Check if constructor has parameters
      if (constructor.ParameterList.Parameters.Count > 0)
      {
        var diagnostic = MapperDiagnostic.MapperConstructorWithParameters(
          constructor.ParameterList.GetLocation(),
          mapperName);

        classDiagnostics.Add(diagnostic);
      }
    }

    private static MapperMethodMetadata? TransformMappingMethod(
      GeneratorAttributeSyntaxContext ctx,
      INamedTypeSymbol mapperDeclaration,
      string mapperName,
      List<MapperDiagnostic> classDiagnostics,
      CancellationToken ct)
    {
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

      if (partialMethods.Count > 0 && !ct.IsCancellationRequested)
      {
        var firstMethod = partialMethods[0];
        var methodTransformer = new MapperMethodTransformer(ctx.SemanticModel);
        return methodTransformer.Transform(firstMethod, ctx.TargetNode, ct);
      }

      return null;
    }

    private bool Predicate(SyntaxNode node, CancellationToken ct) => true;
  }
}
