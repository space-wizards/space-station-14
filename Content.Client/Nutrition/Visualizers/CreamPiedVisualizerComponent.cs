using Content.Shared.Nutrition.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Nutrition.Visualizers;

[RegisterComponent]
[Access(typeof(CreamPiedVisualizerSystem))]
public sealed class CreamPiedVisualizerComponent : Component
{
    [DataField("state")]
    public string? State;
}
