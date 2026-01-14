using System.Collections.Generic;
using System.Threading;

using Mapgen.Analyzer.Mapper.MappingDescriptors;
using Mapgen.Analyzer.Mapper.Utils;

using Microsoft.CodeAnalysis;

namespace Mapgen.Analyzer.Mapper.Strategies;

public sealed class IgnoreMappingStrategy : BaseMappingStrategy
{
  public Dictionary<string, IgnoredPropertyDescriptor> ParseIgnoreMappings(SyntaxNode classNode, CancellationToken ct)
  {
    var ignoreMappings = new Dictionary<string, IgnoredPropertyDescriptor>();

    var constructor = GetMapperConstructor(classNode);
    if (constructor is null)
    {
      return ignoreMappings;
    }

    // Find all IgnoreMember invocations
    var ignoreMemberCalls = FindMethodInvocations(constructor, Constants.IgnoreMemberMethodName);

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
      var destMemberName = SyntaxHelpers.ExtractDestinationPropertyName(destArg);

      if (destMemberName is null || string.IsNullOrEmpty(destMemberName))
      {
        continue;
      }

      var mapping = new IgnoredPropertyDescriptor(destMemberName, ignoreMemberCall.GetLocation());
      ignoreMappings[destMemberName] = mapping;
    }

    return ignoreMappings;
  }
}
