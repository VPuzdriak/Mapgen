using Mapgen.Analyzer;
using Mapgen.Tests.Unit.MappingStrategies.CustomMapping.MultipleParameters.Models;

namespace Mapgen.Tests.Unit.MappingStrategies.CustomMapping.MultipleParameters;

[Mapper]
public partial class ShipmentMapper
{
  public partial ShipmentDto ToDto(Shipment shipment, Party sender, Party receiver);

  public ShipmentMapper()
  {
    MapMember(dto => dto.ShipmentId, shipment => shipment.Id);
    MapMember(dto => dto.SenderName, (_, sender, _) => sender.Name);
    MapMember(dto => dto.ReceiverName, (_, _, receiver) => receiver.Name);
  }
}
