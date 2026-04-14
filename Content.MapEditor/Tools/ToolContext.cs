using Content.MapEditor.Commands;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.MapEditor.Tools;

/// <summary>
///     Shared context passed to editor tools on each mouse event.
/// </summary>
public sealed class ToolContext
{
    public required IEntityManager EntityManager { get; init; }
    public required SharedMapSystem MapSystem { get; init; }
    public required CommandStack CommandStack { get; init; }

    /// <summary>
    ///     The grid currently being edited. Set by the grid tab bar.
    /// </summary>
    public EntityUid ActiveGridUid { get; set; }

    /// <summary>
    ///     The tile type to paint with. Set by the tile palette.
    /// </summary>
    public Tile SelectedTile { get; set; }
}
