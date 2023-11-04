using Content.Server.Power.NodeGroups;
using Robust.Shared.Audio;

namespace Content.Server.Power.Components;

[RegisterComponent]
public sealed partial class SubstationComponent : Component
{

    [DataField("Integrity")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float Integrity = 100.0f;

    [DataField("DecayEnabled")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool DecayEnabled = true;

}
