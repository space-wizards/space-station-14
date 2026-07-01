using Content.Shared.Spreader;

namespace Content.Client.Kudzu;

/// <summary>
///     Entities that have a visual state corresponding to current growth level
///     and random variation, for kudzu tiles.
///    <seealso cref="KudzuVisuals"/>
/// </summary>
[RegisterComponent]
public sealed partial class KudzuVisualsComponent : Component
{
    /// <summary>
    ///     The index of the sprite layer that is reflecting the kudzu's growth state.
    /// </summary>
    [DataField]
    public int Layer { get; private set; } = 0;
}
