using Content.Shared.Actions.ActionTypes;
using Content.Shared.Atmos;
using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

[RegisterComponent, NetworkedComponent]
public sealed class JetpackComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("volumeUsage")]
    public float VolumeUsage = Atmospherics.BreathVolume;

    [ViewVariables, DataField("toggleAction", required: true)]
    public InstantAction ToggleAction = new();

    [ViewVariables(VVAccess.ReadWrite), DataField("acceleration")]
    public float Acceleration = 1f;

    [ViewVariables(VVAccess.ReadWrite), DataField("friction")]
    public float Friction = 0.3f;

    [ViewVariables(VVAccess.ReadWrite), DataField("weightlessModifier")]
    public float WeightlessModifier = 1.2f;
}
