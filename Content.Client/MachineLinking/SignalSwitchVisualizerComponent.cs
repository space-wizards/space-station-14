using Content.Shared.MachineLinking;
using Robust.Client.GameObjects;

namespace Content.Client.MachineLinking;

[RegisterComponent]
[Access(typeof(SignalSwitchVisualizerSystem))]
public sealed class SignalSwitchVisualizerComponent : Component
{
    [DataField("layer")]
    public int Layer { get; }
}
