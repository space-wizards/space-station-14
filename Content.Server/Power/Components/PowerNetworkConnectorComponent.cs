using Content.Server.Power.NodeGroups;
using Content.Shared.Power;

namespace Content.Server.Power.Components;

[RegisterComponent]
public sealed partial class PowerNetworkConnectorComponent : Component
{
    [DataField]
    public Voltage Voltage;

    public PowerNet? Net;

    [DataField("node")]
    public string? NodeId;
}
