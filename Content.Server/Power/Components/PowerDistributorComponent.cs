using Content.Shared.Power;

namespace Content.Server.Power.Components;

[RegisterComponent]
public sealed partial class PowerDistributorComponent : Component
{
    [DataField("sourceNode")]
    public string SourceNode = "hv";

    [DataField("loadNode")]
    public string LoadNode = "hv";

    [DataField("lastExternalState")]
    public PowerDistributorExternalPowerState LastExternalState;
}

