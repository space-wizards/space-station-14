using Content.Server.Power.NodeGroups;
using Robust.Shared.Audio;

namespace Content.Server.Power.Components;

[RegisterComponent]
public sealed partial class SubstationComponent : Component
{

    [DataField("integrity")]
    public float Integrity = 100.0f;

    [DataField("enabled")]
    public bool Enabled = true;

}
