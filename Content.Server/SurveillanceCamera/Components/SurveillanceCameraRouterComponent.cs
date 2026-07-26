using Content.Shared.DeviceNetwork;
using Robust.Shared.Prototypes;

namespace Content.Server.SurveillanceCamera;

[RegisterComponent]
public sealed partial class SurveillanceCameraRouterComponent : Component
{
    [ViewVariables] public bool Active { get; set; }

    // The name of the subnet connected to this router.
    [ViewVariables]
    public string SubnetName = string.Empty;

    /// <summary>
    /// The monitors to route to. This raises an issue related to
    /// camera monitors disappearing before sending a D/C packet,
    /// this could probably be refreshed every time a new monitor
    /// is added or removed from active routing.
    /// </summary>
    [ViewVariables]
    public HashSet<string> MonitorRoutes { get; } = new();

    /// <summary>
    /// The frequency that talks to this router's subnet.
    /// </summary>
    [ViewVariables]
    public uint SubnetFrequency;

    [DataField("subnetFrequency")]
    public ProtoId<DeviceFrequencyPrototype>? SubnetFrequencyId { get; set;  }

    [DataField("setupAvailableNetworks")]
    public List<ProtoId<DeviceFrequencyPrototype>> AvailableNetworks { get; private set; } = new();
}
