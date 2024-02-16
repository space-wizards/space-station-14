using Content.Shared.DeviceNetwork;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.SurveillanceCamera;

[RegisterComponent]
public sealed partial class SurveillanceCameraRouterComponent : Component
{
    [ViewVariables] public bool Active { get; set; }

    // The name of the subnet connected to this router.
    [DataField("subnetName")]
    public string SubnetName { get; set; } = string.Empty;

    [ViewVariables]
    // The monitors to route to. This raises an issue related to
    // camera monitors disappearing before sending a D/C packet,
    // this could probably be refreshed every time a new monitor
    // is added or removed from active routing.
    public HashSet<string> MonitorRoutes { get; } = new();

    [ViewVariables]
    // The frequency that talks to this router's subnet.
    public uint SubnetFrequency;
    [DataField("subnetFrequency", customTypeSerializer:typeof(PrototypeIdSerializer<DeviceFrequencyPrototype>))]
    public string? SubnetFrequencyId { get; set;  }

    [DataField("setupAvailableNetworks", customTypeSerializer:typeof(PrototypeIdListSerializer<DeviceFrequencyPrototype>))]
    public List<string> AvailableNetworks { get; private set; } = new();
}
