using Content.Shared.Nutrition.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Nutrition.Visualizers;

[RegisterComponent]
[Access(typeof(DrinkCanVisualizerSystem))]
public sealed class DrinkCanVisualizerComponent : Component
{
    [DataField("stateClosed")]
    public string? StateClosed;

    [DataField("stateOpen")]
    public string? StateOpen;
}
