using Microsoft.CodeAnalysis;

namespace Mapgen.Analyzer.Mapper.MappingDescriptors
{
  public sealed class IgnoredPropertyDescriptor : BaseMappingDescriptor
  {
    public Location? IgnoreMemberMethodCallLocation { get; }

    public IgnoredPropertyDescriptor(string targetPropertyName, Location? ignoreMemberMethodCallLocation)
      : base(targetPropertyName)
    {
      IgnoreMemberMethodCallLocation = ignoreMemberMethodCallLocation;
    }
  }
}
