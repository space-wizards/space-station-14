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

    /// <summary>
    ///     Clipboard data for copy/paste operations. Shared across tool instances.
    /// </summary>
    public ClipboardData? Clipboard { get; set; }

    /// <summary>
    ///     The entity prototype ID to place. Set by the entity palette.
    /// </summary>
    public string? SelectedEntityPrototype { get; set; }

    /// <summary>
    ///     The cable prototype ID to draw (e.g. "CableHV", "CableMV", "CableApcExtension").
    ///     Set by the infrastructure panel when a cable type is selected.
    /// </summary>
    public string? SelectedCablePrototype { get; set; }

    /// <summary>
    ///     The pipe prototype ID to draw. Set by the infrastructure panel.
    /// </summary>
    public string? SelectedPipePrototype { get; set; }

    /// <summary>
    ///     Whether infrastructure mode is currently active.
    ///     Tools can use this to adjust behavior (e.g. erase only infra entities).
    /// </summary>
    public bool InfrastructureMode { get; set; }

    /// <summary>
    ///     Rotation applied to placed pipes (in radians). Cycled with R/Shift+R.
    ///     0 = default, π/2 = 90° CW, π = 180°, 3π/2 = 270°.
    /// </summary>
    public Robust.Shared.Maths.Angle PlacementRotation { get; set; }
}
