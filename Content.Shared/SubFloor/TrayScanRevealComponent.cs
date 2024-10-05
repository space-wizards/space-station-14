using Robust.Shared.GameStates;
using Robust.Shared.Map.Components;

namespace Content.Shared.SubFloor;

/// <summary>
/// For tile-like entities that should reveal subfloor entities when scanning with t-rays.
/// </summary>
[RegisterComponent]
[NetworkedComponent]
public sealed partial class TrayScanRevealComponent : Component
{
    public (EntityUid, MapGridComponent, Vector2i) Tile;
}
