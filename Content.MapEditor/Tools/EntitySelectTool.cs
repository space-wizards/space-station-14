using System;
using System.Collections.Generic;
using Content.MapEditor.Commands;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.MapEditor.Tools;

/// <summary>
///     Selects an entity at the clicked tile position. Supports delete, rotation,
///     and drag-to-move of the selected entity.
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

    // Drag state
    private Vector2i _dragStartTile;
    private Vector2i _lastDragTile;
    private EntityCoordinates _dragStartCoords;

    public void OnMouseDown(ToolContext ctx, Vector2i tilePos)
    {
        var gridUid = ctx.ActiveGridUid;
        var grid = ctx.EntityManager.GetComponent<MapGridComponent>(gridUid);

        // Collect all anchored entities at this tile.
        var entities = new List<EntityUid>();
        foreach (var ent in ctx.MapSystem.GetAnchoredEntities(gridUid, grid, tilePos))
        {
            if (ctx.EntityManager.EntityExists(ent))
                entities.Add(ent);
        }

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
        if (entities.Count > 0)
        {
            // When multiple entities occupy the same tile, pick the first one.
            // TODO: stack picker popup for multi-entity tiles.
            SelectedEntity = entities[0];
            SelectedTilePos = tilePos;
        }
        else
        {
            SelectedEntity = null;
            SelectedTilePos = null;
        }
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
