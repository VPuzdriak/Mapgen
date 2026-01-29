namespace Mapgen.Analyzer.Mapper.MappingDescriptors
{
  public sealed class MappingDescriptor : SourceMappingDescriptor
  {
    public MappingDescriptor(string targetPropertyName, string sourceExpression)
      : base(targetPropertyName, sourceExpression)
    {
    }
  }
}
