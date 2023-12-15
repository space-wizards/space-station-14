using Content.Shared.Singularity.Components;

namespace Content.Client.ParticleAccelerator;

[RegisterComponent]
[Access(typeof(ParticleAcceleratorPartVisualizerSystem))]
public sealed partial class ParticleAcceleratorPartVisualsComponent : Component
{
    [DataField("stateBase", required: true)]
    [ViewVariables(VVAccess.ReadWrite)]
    public string StateBase = default!;

    [DataField("stateSuffixes")]
    [ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<ParticleAcceleratorVisualState, string> StatesSuffixes = new()
    {
        {ParticleAcceleratorVisualState.Powered, "p"},
        {ParticleAcceleratorVisualState.Level0, "p0"},
        {ParticleAcceleratorVisualState.Level1, "p1"},
        {ParticleAcceleratorVisualState.Level2, "p2"},
        {ParticleAcceleratorVisualState.Level3, "p3"},
    };
}
