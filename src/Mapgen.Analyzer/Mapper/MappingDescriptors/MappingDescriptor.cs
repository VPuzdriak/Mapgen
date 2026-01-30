namespace Mapgen.Analyzer.Mapper.MappingDescriptors
{
  public sealed class MappingDescriptor : SourceMappingDescriptor
  {
    public MappingDescriptor(string targetMemberName, string sourceExpression)
      : base(targetMemberName, sourceExpression)
    {
    }
  }
}
