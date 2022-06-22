using Content.Shared.Actions.ActionTypes;
using Content.Shared.Atmos;
using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

[RegisterComponent, NetworkedComponent]
public sealed class JetpackComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("enabled")]
    public bool Enabled = false;

    [ViewVariables(VVAccess.ReadWrite), DataField("volumeUsage")]
    public float VolumeUsage = Atmospherics.BreathVolume;

    [ViewVariables, DataField("toggleAction", required: true)]
    public InstantAction ToggleAction = new();
}
