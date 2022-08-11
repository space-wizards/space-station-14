using Content.Shared.Shuttles.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Shuttles.Components;

/// <summary>
/// Handles what a grid should look like on radar.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedShuttleSystem))]
public sealed class IFFComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("flags")]
    public IFFFlags Flags = IFFFlags.None;
}

[Flags]
public enum IFFFlags : byte
{
    None = 0,

    /// <summary>
    /// Should the label for this grid be hidden at all ranges.
    /// </summary>
    HideLabel,

    // TODO: Need one that hides its outline, just replace it with a bunch of triangles or lines or something.
}
