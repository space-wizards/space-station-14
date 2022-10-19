using Content.Shared.Radiation.Components;

namespace Content.Client.Radiation.Components;

/// <summary>
///     Visualizer for generic geiger counter.
/// </summary>
[RegisterComponent]
public sealed class GeigerVisualsComponent : Component
{
    /// <summary>
    ///     Sprite states mapped by radiation danger level.
    /// </summary>
    [DataField("states")]
    public Dictionary<GeigerDangerLevel, string> States = new();
}
