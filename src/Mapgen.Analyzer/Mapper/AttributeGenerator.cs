using Microsoft.CodeAnalysis;

namespace Mapgen.Analyzer.Mapper;

[Generator]
public class AttributeGenerator : IIncrementalGenerator
{
  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    // Generate the MapperAttribute on every compilation
    context.RegisterPostInitializationOutput(ctx =>
    {
      ctx.AddSource($"{MapgenProject.MapperAttributeClassName}.g.cs",
        $$"""

        using System;

        namespace {{MapgenProject.MapperAttributeNamespace}}
        {
            /// <summary>
            /// Marks a partial class as a mapper.
            /// The source generator will automatically implement mapping methods.
            /// </summary>
            [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
            public sealed class {{MapgenProject.MapperAttributeClassName}} : Attribute
            {
                /// <summary>
                /// Gets or sets a value indicating whether to use fully qualified type names in generated code.
                /// When set to true, all type references in the generated code will use their full namespace-qualified names.
                /// Default is false.
                /// </summary>
                public bool UseFullNameQualifiers { get; set; }
            }
        }

        """);
    });
  }
}
