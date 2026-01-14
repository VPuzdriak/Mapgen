using Mapgen.Analyzer.Mapper.Metadata;

using Microsoft.CodeAnalysis;

namespace Mapgen.Analyzer.Mapper.Diagnostics
{
  public class MapperDiagnosticsReporter
  {
    public void Report(SourceProductionContext ctx, MappingConfigurationMetadata configMetadata)
    {
      // Report class-level diagnostics (e.g., multiple mapping methods)
      foreach (var mapperDiagnostic in configMetadata.Diagnostics)
      {
        var descriptor = new DiagnosticDescriptor(
          id: mapperDiagnostic.Id,
          title: mapperDiagnostic.Title,
          messageFormat: mapperDiagnostic.MessageFormat,
          category: DiagnosticIds.Category,
          mapperDiagnostic.Severity,
          isEnabledByDefault: true);

        var diagnostic = Diagnostic.Create(
          descriptor,
          mapperDiagnostic.Location,
          mapperDiagnostic.MessageArgs);

        ctx.ReportDiagnostic(diagnostic);
      }

      // Report method-level diagnostics (e.g., unmapped properties, lambda blocks)
      if (configMetadata.Method is not null)
      {
        foreach (var mapperDiagnostic in configMetadata.Method.Diagnostics)
        {
          var descriptor = new DiagnosticDescriptor(
            id: mapperDiagnostic.Id,
            title: mapperDiagnostic.Title,
            messageFormat: mapperDiagnostic.MessageFormat,
            category: DiagnosticIds.Category,
            mapperDiagnostic.Severity,
            isEnabledByDefault: true);

          var diagnostic = Diagnostic.Create(
            descriptor,
            mapperDiagnostic.Location,
            mapperDiagnostic.MessageArgs);

          ctx.ReportDiagnostic(diagnostic);
        }
      }
    }
  }
}
