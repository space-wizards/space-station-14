using System;
using System.Collections.Generic;
using System.Numerics;
using Content.MapEditor.Commands;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.MapEditor.Tools;

/// <summary>
///     Selects an entity at the clicked tile position. Supports delete, rotation,
///     and drag-to-move of the selected entity.
///     When multiple entities occupy the same tile, exposes <see cref="PendingPick"/>
///     so the UI can show a stack picker popup.
/// </summary>
public sealed class EntitySelectTool : IEditorTool
{
    public string Name => "Entity Select";

    /// <summary>
    ///     The currently selected entity, or null if nothing is selected.
    /// </summary>
    public EntityUid? SelectedEntity { get; private set; }

    /// <summary>
    ///     The tile position of the selected entity, for overlay highlighting.
    /// </summary>
    public Vector2i? SelectedTilePos { get; private set; }

    /// <summary>True while the user is dragging the selected entity to a new tile.</summary>
    public bool IsDragging { get; private set; }

    /// <summary>
    ///     When a click finds multiple entities at the same tile, this list is populated
    ///     so the UI layer can show a picker popup. Cleared after the pick is resolved.
    /// </summary>
    public List<EntityUid>? PendingPick { get; private set; }

    /// <summary>
    ///     The tile position associated with the pending pick, used to set
    ///     <see cref="SelectedTilePos"/> after the user picks an entity.
    /// </summary>
    public Vector2i? PendingPickTilePos { get; private set; }

    // Drag state
    private Vector2i _dragStartTile;
    private Vector2i _lastDragTile;
    private EntityCoordinates _dragStartCoords;

    public void OnMouseDown(ToolContext ctx, Vector2i tilePos)
    {
        // Clear any stale pending pick.
        PendingPick = null;
        PendingPickTilePos = null;

        var gridUid = ctx.ActiveGridUid;
        var grid = ctx.EntityManager.GetComponent<MapGridComponent>(gridUid);

        // Collect all entities at this tile: anchored + non-anchored via spatial lookup.
        var entities = new List<EntityUid>();

        // 1. Anchored entities (the fast path — grid-indexed).
        foreach (var ent in ctx.MapSystem.GetAnchoredEntities(gridUid, grid, tilePos))
        {
            if (ctx.EntityManager.EntityExists(ent))
                entities.Add(ent);
        }

        // 2. Non-anchored entities found via EntityLookupSystem spatial query.
        //    Build a world-space AABB for the tile and query all uncontained entities.
        var lookup = ctx.EntityManager.System<EntityLookupSystem>();
        var xformSystem = ctx.EntityManager.System<SharedTransformSystem>();

        // Tile covers [tilePos.X, tilePos.X+1) x [tilePos.Y, tilePos.Y+1) in grid-local space.
        // Transform to world space using the grid's world matrix.
        var tileSize = grid.TileSize;
        var localMin = new Vector2(tilePos.X * tileSize, tilePos.Y * tileSize);
        var localMax = new Vector2((tilePos.X + 1) * tileSize, (tilePos.Y + 1) * tileSize);

        var worldMatrix = xformSystem.GetWorldMatrix(gridUid);
        var worldMin = Vector2.Transform(localMin, worldMatrix);
        var worldMax = Vector2.Transform(localMax, worldMatrix);

        // Ensure min/max are correct after transform (rotation could swap them).
        var worldBox = new Box2(
            MathF.Min(worldMin.X, worldMax.X),
            MathF.Min(worldMin.Y, worldMax.Y),
            MathF.Max(worldMin.X, worldMax.X),
            MathF.Max(worldMin.Y, worldMax.Y));

        var nonAnchored = lookup.GetEntitiesIntersecting(gridUid, worldBox,
            LookupFlags.Dynamic | LookupFlags.Sundries | LookupFlags.Static);

        foreach (var ent in nonAnchored)
        {
            if (ctx.EntityManager.EntityExists(ent) && !entities.Contains(ent))
                entities.Add(ent);
        }

        // Filter out the grid entity itself and map entities.
        entities.RemoveAll(e =>
            e == gridUid
            || ctx.EntityManager.HasComponent<MapGridComponent>(e)
            || ctx.EntityManager.HasComponent<MapComponent>(e));

        // If clicking on a tile that contains the already-selected entity, start a drag move.
        if (SelectedEntity != null
            && ctx.EntityManager.EntityExists(SelectedEntity.Value)
            && entities.Contains(SelectedEntity.Value))
        {
            IsDragging = true;
            _dragStartTile = tilePos;
            _lastDragTile = tilePos;
            _dragStartCoords = ctx.EntityManager.GetComponent<TransformComponent>(SelectedEntity.Value).Coordinates;
            return;
        }

        // Select a new entity (or clear selection).
        if (entities.Count > 1)
        {
            // Multiple entities — expose them for a picker popup.
            PendingPick = entities;
            PendingPickTilePos = tilePos;
        }
        else if (entities.Count == 1)
        {
            SelectedEntity = entities[0];
            SelectedTilePos = tilePos;
        }
        else
        {
            SelectedEntity = null;
            SelectedTilePos = null;
        }
    }

    /// <summary>
    ///     Called by the UI after the user picks an entity from the stack picker popup.
    /// </summary>
    public void ConfirmPick(EntityUid uid)
    {
        SelectedEntity = uid;
        SelectedTilePos = PendingPickTilePos;
        PendingPick = null;
        PendingPickTilePos = null;
    }

    /// <summary>
    ///     Cancels a pending pick without selecting anything.
    /// </summary>
    public void CancelPick()
    {
        PendingPick = null;
        PendingPickTilePos = null;
    }

    public void OnMouseDrag(ToolContext ctx, Vector2i tilePos)
    {
        if (!IsDragging || SelectedEntity == null || !ctx.EntityManager.EntityExists(SelectedEntity.Value))
            return;

        if (tilePos == _lastDragTile)
            return;

        _lastDragTile = tilePos;

        // Move the entity visually during the drag (final command applied on mouse up).
        var gridUid = ctx.ActiveGridUid;
        var grid = ctx.EntityManager.GetComponent<MapGridComponent>(gridUid);
        var newCoords = ctx.MapSystem.GridTileToLocal(gridUid, grid, tilePos);

        var xform = ctx.EntityManager.GetComponent<TransformComponent>(SelectedEntity.Value);
        xform.Coordinates = newCoords;

        SelectedTilePos = tilePos;
    }

    public void OnMouseUp(ToolContext ctx)
    {
        if (!IsDragging || SelectedEntity == null)
        {
            IsDragging = false;
            return;
        }

        IsDragging = false;

        if (!ctx.EntityManager.EntityExists(SelectedEntity.Value))
            return;

        // Only create a command if the entity actually moved.
        if (_lastDragTile == _dragStartTile)
            return;

        var gridUid = ctx.ActiveGridUid;
        var grid = ctx.EntityManager.GetComponent<MapGridComponent>(gridUid);
        var newCoords = ctx.MapSystem.GridTileToLocal(gridUid, grid, _lastDragTile);

        // Revert to start position so Execute() applies the move cleanly for undo/redo.
        var xform = ctx.EntityManager.GetComponent<TransformComponent>(SelectedEntity.Value);
        xform.Coordinates = _dragStartCoords;

        var cmd = new MoveEntityCommand(ctx.EntityManager, SelectedEntity.Value, _dragStartCoords, newCoords);
        ctx.CommandStack.Execute(cmd);

        SelectedTilePos = _lastDragTile;
    }

    /// <summary>
    ///     Deletes the selected entity and pushes an undo command.
    /// </summary>
    public void DeleteSelected(ToolContext ctx)
    {
        if (SelectedEntity == null || !ctx.EntityManager.EntityExists(SelectedEntity.Value))
        {
            // Stale selection — clear it.
            SelectedEntity = null;
            SelectedTilePos = null;
            return;
        }

        var cmd = new DeleteEntityCommand(ctx.EntityManager, SelectedEntity.Value);
        cmd.Execute();
        ctx.CommandStack.Push(cmd);

        SelectedEntity = null;
        SelectedTilePos = null;
    }

    /// <summary>
    ///     Rotates the selected entity clockwise by 90 degrees.
    /// </summary>
    public void RotateSelectedCW(ToolContext ctx)
    {
        RotateSelected(ctx, -Math.PI / 2);
    }

    /// <summary>
    ///     Rotates the selected entity counter-clockwise by 90 degrees.
    /// </summary>
    public void RotateSelectedCCW(ToolContext ctx)
    {
        RotateSelected(ctx, Math.PI / 2);
    }

    private void RotateSelected(ToolContext ctx, double radians)
    {
        if (SelectedEntity == null || !ctx.EntityManager.EntityExists(SelectedEntity.Value))
            return;

        var cmd = new RotateEntityCommand(ctx.EntityManager, SelectedEntity.Value, new Angle(radians));
        ctx.CommandStack.Execute(cmd);
    }
}
