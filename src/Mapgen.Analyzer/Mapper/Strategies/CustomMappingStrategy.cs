using System.Collections.Generic;
using System.Threading;

using Mapgen.Analyzer.Mapper.MappingDescriptors;
using Mapgen.Analyzer.Mapper.Metadata;
using Mapgen.Analyzer.Mapper.Utils;

using Microsoft.CodeAnalysis;

namespace Mapgen.Analyzer.Mapper.Strategies;

public sealed class CustomMappingStrategy : BaseMappingStrategy
{
  public Dictionary<string, BaseMappingDescriptor> ParseCustomMappings(
    SyntaxNode classNode,
    MapperMethodMetadata methodMetadata,
    CancellationToken ct)
  {
    var customMappings = new Dictionary<string, BaseMappingDescriptor>();

    var constructor = GetMapperConstructor(classNode);
    if (constructor is null)
    {
      return customMappings;
    }

    // Find all MapMember invocations
    var mapMemberCalls = FindMethodInvocations(constructor, MappingConfigurationMethods.MapMemberMethodName);

    foreach (var mapMemberCall in mapMemberCalls)
    {
      if (ct.IsCancellationRequested)
      {
        break;
      }

      // E.G. MapMember(carDto => carDto.CarId, car => car.Id)
      if (mapMemberCall.ArgumentList.Arguments.Count != 2)
      {
        continue;
      }

      // First argument: destination member (e.g., carDto => carDto.CarId)
      var destArg = mapMemberCall.ArgumentList.Arguments[0].Expression;
      var destPropertyName = SyntaxHelpers.ExtractDestinationPropertyName(destArg);

      if (destPropertyName is null or "")
      {
        continue;
      }

      // Second argument: source expression (e.g., car => car.Id or car => car.Name + "Model")
      var sourceArg = mapMemberCall.ArgumentList.Arguments[1].Expression;

      // Validate lambda expression body (blocks not supported)
      if (!ValidateLambdaExpressionBody(
        sourceArg,
        destPropertyName,
        MappingConfigurationMethods.MapMemberMethodName,
        mapMemberCall.GetLocation(),
        methodMetadata))
      {
        // Add a diagnosed property descriptor to prevent "missing property" diagnostic
        customMappings[destPropertyName] = new DiagnosedPropertyDescriptor(destPropertyName);
        continue;
      }

      var sourceExpression = LambdaExpressionExtractor.ExtractSourceExpression(sourceArg, methodMetadata);

      if (sourceExpression is null or "")
      {
        continue;
      }

      var mapping = new MappingDescriptor(destPropertyName, sourceExpression);
      customMappings[destPropertyName] = mapping;
    }

    return customMappings;
  }
}
