using System;
using Content.MapEditor.Commands;
using Content.Shared.Maps;
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
    public required ITileDefinitionManager TileDefinitionManager { get; init; }

    private readonly Random _variantRng = new();

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

    /// <summary>
    ///     Exact cursor world position (not snapped to tile grid).
    ///     Used for free placement when Shift is held.
    /// </summary>
    public System.Numerics.Vector2 CursorWorldPosition { get; set; }

    /// <summary>
    ///     Whether Shift is held (enables free placement mode).
    /// </summary>
    public bool ShiftHeld { get; set; }

    /// <summary>
    ///     Returns a copy of <see cref="SelectedTile"/> with a random variant chosen
    ///     using the tile definition's <see cref="ContentTileDefinition.PlacementVariants"/> weights.
    /// </summary>
    public Tile GetVariantTile()
    {
        var tileId = SelectedTile.TypeId;
        if (tileId <= 0 || !TileDefinitionManager.TryGetDefinition(tileId, out var def))
            return SelectedTile;

        if (def is not ContentTileDefinition contentDef || contentDef.Variants <= 1)
            return SelectedTile;

        var variant = PickVariant(contentDef);
        return new Tile(tileId, variant: variant);
    }

    private byte PickVariant(ContentTileDefinition tileDef)
    {
        var variants = tileDef.PlacementVariants;
        var sum = 0f;
        foreach (var w in variants)
            sum += w;

        var rand = (float)(_variantRng.NextDouble() * sum);
        var accumulated = 0f;

        for (byte i = 0; i < variants.Length; i++)
        {
            accumulated += variants[i];
            if (accumulated >= rand)
                return i;
        }

        return 0;
    }
}
