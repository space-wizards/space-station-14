using Content.Shared.Singularity.Components;

namespace Content.Client.ParticleAccelerator;

[RegisterComponent]
[Access(typeof(ParticleAcceleratorPartVisualizerSystem))]
public sealed class ParticleAcceleratorPartVisualizerComponent : Component
{
    [DataField("baseState", required: true)]
    public string BaseState = default!;

    public static readonly Dictionary<ParticleAcceleratorVisualState, string> StatesSuffixes = new()
    {
        {ParticleAcceleratorVisualState.Powered, "p"},
        {ParticleAcceleratorVisualState.Level0, "p0"},
        {ParticleAcceleratorVisualState.Level1, "p1"},
        {ParticleAcceleratorVisualState.Level2, "p2"},
        {ParticleAcceleratorVisualState.Level3, "p3"},
    };
}
