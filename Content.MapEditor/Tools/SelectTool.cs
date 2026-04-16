using System;
using System.Collections.Generic;
using Content.MapEditor.Commands;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.MapEditor.Tools;

/// <summary>
///     Click-and-drag to select a rectangular region. Supports copy, cut, paste, and delete
///     operations on the selected tiles.
/// </summary>
public sealed class SelectTool : IEditorTool
{
    public string Name => "Select";

    private Vector2i _dragStart;
    private Vector2i _dragEnd;
    private bool _isDragging;
    private bool _isMoving;
    private Vector2i _moveOrigin;

    /// <summary>
    ///     The finalized selection rectangle, or null if nothing is selected.
    /// </summary>
    public Box2i? Selection { get; private set; }

    /// <summary>True if currently moving the selection.</summary>
    public bool IsMoving => _isMoving;

    /// <summary>Start corner of the in-progress drag, exposed for overlay preview.</summary>
    public Vector2i? DragStart => _isDragging ? _dragStart : null;

    /// <summary>Current end corner of the in-progress drag, exposed for overlay preview.</summary>
    public Vector2i? DragEnd => _isDragging ? _dragEnd : null;

    // For move mode: track the original selection and accumulated offset.
    private Box2i _originalSelection;
    private Vector2i _totalMoveOffset;
    private List<EntityUid>? _moveEntities;

    /// <summary>
    ///     Tile positions (in grid coords) that were non-empty when the move started.
    ///     Used by the overlay to render ghost tiles during drag.
    /// </summary>
    public System.Collections.Generic.List<Vector2i>? MoveGhostTiles { get; private set; }

    /// <summary>Current move offset from the original position, for ghost rendering.</summary>
    public Vector2i MoveOffset => _totalMoveOffset;

    public void OnMouseDown(ToolContext ctx, Vector2i tilePos)
    {
        // If clicking inside an existing selection, enter move mode.
        if (Selection != null && Selection.Value.Contains(tilePos))
        {
            _isMoving = true;
            _moveOrigin = tilePos;
            _originalSelection = Selection.Value;
            _totalMoveOffset = Vector2i.Zero;

            // Snapshot non-empty tile positions and ALL entities for ghost rendering and move.
            MoveGhostTiles = new List<Vector2i>();
            _moveEntities = new List<EntityUid>();
            var gridUid = ctx.ActiveGridUid;
            var grid = ctx.EntityManager.GetComponent<MapGridComponent>(gridUid);
            var entitySet = new HashSet<EntityUid>();

            for (var x = _originalSelection.Left; x < _originalSelection.Right; x++)
            {
                for (var y = _originalSelection.Bottom; y < _originalSelection.Top; y++)
                {
                    var pos = new Vector2i(x, y);
                    if (ctx.MapSystem.GetTileRef(gridUid, grid, pos).Tile != Tile.Empty)
                        MoveGhostTiles.Add(pos);

                    // Collect anchored entities at this tile.
                    foreach (var ent in ctx.MapSystem.GetAnchoredEntities(gridUid, grid, pos))
                    {
                        if (ctx.EntityManager.EntityExists(ent))
                            entitySet.Add(ent);
                    }
                }
            }

            // Also collect non-anchored entities via spatial lookup.
            var xformSystem = ctx.EntityManager.System<SharedTransformSystem>();
            var allQuery = ctx.EntityManager.AllEntityQueryEnumerator<TransformComponent>();
            while (allQuery.MoveNext(out var uid, out var xform))
            {
                if (xform.GridUid != gridUid || entitySet.Contains(uid))
                    continue;
                if (ctx.EntityManager.HasComponent<MapGridComponent>(uid) || ctx.EntityManager.HasComponent<MapComponent>(uid))
                    continue;

                var entTile = ctx.MapSystem.CoordinatesToTile(gridUid, grid, xform.Coordinates);
                if (entTile.X >= _originalSelection.Left && entTile.X < _originalSelection.Right
                    && entTile.Y >= _originalSelection.Bottom && entTile.Y < _originalSelection.Top)
                {
                    entitySet.Add(uid);
                }
            }

            _moveEntities = new List<EntityUid>(entitySet);
            return;
        }

        // Otherwise start a new selection drag.
        _isDragging = true;
        _isMoving = false;
        _dragStart = tilePos;
        _dragEnd = tilePos;
        Selection = null;
    }

    public void OnMouseDrag(ToolContext ctx, Vector2i tilePos)
    {
        if (_isMoving)
        {
            // Update visual selection position as user drags.
            var delta = tilePos - _moveOrigin;
            _totalMoveOffset += delta;
            Selection = new Box2i(
                Selection!.Value.Left + delta.X,
                Selection.Value.Bottom + delta.Y,
                Selection.Value.Right + delta.X,
                Selection.Value.Top + delta.Y);
            _moveOrigin = tilePos;
            return;
        }

        _dragEnd = tilePos;
    }

    public void OnMouseUp(ToolContext ctx)
    {
        if (_isMoving)
        {
            _isMoving = false;
            MoveGhostTiles = null;

            // Apply the tile + entity move if there was actual displacement.
            if (_totalMoveOffset != Vector2i.Zero)
                ApplyMove(ctx, _originalSelection, _totalMoveOffset);
            return;
        }

        if (!_isDragging)
            return;

        _isDragging = false;

        var minX = Math.Min(_dragStart.X, _dragEnd.X);
        var minY = Math.Min(_dragStart.Y, _dragEnd.Y);
        var maxX = Math.Max(_dragStart.X, _dragEnd.X);
        var maxY = Math.Max(_dragStart.Y, _dragEnd.Y);
        Selection = new Box2i(minX, minY, maxX + 1, maxY + 1);
    }

    /// <summary>
    ///     Moves tiles and entities from the original selection area by the given offset.
    ///     Clears the source tiles and places them at the destination.
    /// </summary>
    private void ApplyMove(ToolContext ctx, Box2i source, Vector2i offset)
    {
        var gridUid = ctx.ActiveGridUid;
        var grid = ctx.EntityManager.GetComponent<MapGridComponent>(gridUid);

        var batch = new BatchCommand();

        // Collect source tiles first (before modifying anything).
        var tiles = new System.Collections.Generic.Dictionary<Vector2i, Tile>();
        for (var x = source.Left; x < source.Right; x++)
        {
            for (var y = source.Bottom; y < source.Top; y++)
            {
                var pos = new Vector2i(x, y);
                var tile = ctx.MapSystem.GetTileRef(gridUid, grid, pos).Tile;
                if (tile != Tile.Empty)
                    tiles[pos] = tile;
            }
        }

        // Clear source positions.
        foreach (var (pos, tile) in tiles)
        {
            var cmd = new SetTileCommand(ctx.MapSystem, gridUid, grid, pos, tile, Tile.Empty);
            cmd.Execute();
            batch.Add(cmd);
        }

        // Place at destination positions.
        foreach (var (pos, tile) in tiles)
        {
            var destPos = pos + offset;
            var oldDest = ctx.MapSystem.GetTileRef(gridUid, grid, destPos).Tile;
            var cmd = new SetTileCommand(ctx.MapSystem, gridUid, grid, destPos, oldDest, tile);
            cmd.Execute();
            batch.Add(cmd);
        }

        // Move entities that were in the selection.
        // Temporarily disable physics collision on each entity to prevent broadphase
        // assertion crashes, then restore after repositioning.
        if (_moveEntities != null)
        {
            var worldOffset = new System.Numerics.Vector2(offset.X, offset.Y);

            foreach (var entUid in _moveEntities)
            {
                if (!ctx.EntityManager.EntityExists(entUid))
                    continue;

                var xform = ctx.EntityManager.GetComponent<TransformComponent>(entUid);
                var oldCoords = xform.Coordinates;
                var newPos = oldCoords.Position + worldOffset;
                var newCoords = new EntityCoordinates(oldCoords.EntityId, newPos);

                var cmd = new MoveEntityCommand(ctx.EntityManager, entUid, oldCoords, newCoords);
                cmd.Execute();
                batch.Add(cmd);
            }
        }

        if (batch.Count > 0)
            ctx.CommandStack.Push(batch);
    }

    /// <summary>
    ///     Deletes all tiles in the selection by setting them to <see cref="Tile.Empty"/>.
    /// </summary>
    public void DeleteSelection(ToolContext ctx)
    {
        if (Selection == null)
            return;

        var sel = Selection.Value;
        var gridUid = ctx.ActiveGridUid;
        var grid = ctx.EntityManager.GetComponent<MapGridComponent>(gridUid);
        var batch = new BatchCommand();

        for (var x = sel.Left; x < sel.Right; x++)
        {
            for (var y = sel.Bottom; y < sel.Top; y++)
            {
                var pos = new Vector2i(x, y);
                var oldTile = ctx.MapSystem.GetTileRef(gridUid, grid, pos).Tile;

                if (oldTile == Tile.Empty)
                    continue;

                var cmd = new SetTileCommand(ctx.MapSystem, gridUid, grid, pos, oldTile, Tile.Empty);
                cmd.Execute();
                batch.Add(cmd);
            }
        }

        if (batch.Count > 0)
            ctx.CommandStack.Push(batch);

        Selection = null;
    }

    /// <summary>
    ///     Copies the tiles in the selection to the clipboard.
    /// </summary>
    public void CopySelection(ToolContext ctx)
    {
        if (Selection == null)
            return;

        var sel = Selection.Value;
        var gridUid = ctx.ActiveGridUid;
        var grid = ctx.EntityManager.GetComponent<MapGridComponent>(gridUid);
        var clipboard = new ClipboardData
        {
            Size = new Vector2i(sel.Right - sel.Left, sel.Top - sel.Bottom),
        };

        for (var x = sel.Left; x < sel.Right; x++)
        {
            for (var y = sel.Bottom; y < sel.Top; y++)
            {
                var pos = new Vector2i(x, y);
                var tile = ctx.MapSystem.GetTileRef(gridUid, grid, pos).Tile;

                if (tile != Tile.Empty)
                {
                    var offset = new Vector2i(x - sel.Left, y - sel.Bottom);
                    clipboard.Tiles[offset] = tile;
                }
            }
        }

        ctx.Clipboard = clipboard;
    }

    /// <summary>
    ///     Copies the selection to the clipboard, then deletes the tiles.
    /// </summary>
    public void CutSelection(ToolContext ctx)
    {
        CopySelection(ctx);
        DeleteSelection(ctx);
    }

    /// <summary>
    ///     Pastes clipboard contents at the given tile position.
    /// </summary>
    public void PasteClipboard(ToolContext ctx, Vector2i position)
    {
        if (ctx.Clipboard == null || ctx.Clipboard.Tiles.Count == 0)
            return;

        var gridUid = ctx.ActiveGridUid;
        var grid = ctx.EntityManager.GetComponent<MapGridComponent>(gridUid);
        var batch = new BatchCommand();

        foreach (var (offset, newTile) in ctx.Clipboard.Tiles)
        {
            var pos = new Vector2i(position.X + offset.X, position.Y + offset.Y);
            var oldTile = ctx.MapSystem.GetTileRef(gridUid, grid, pos).Tile;

            if (oldTile == newTile)
                continue;

            var cmd = new SetTileCommand(ctx.MapSystem, gridUid, grid, pos, oldTile, newTile);
            cmd.Execute();
            batch.Add(cmd);
        }

        if (batch.Count > 0)
            ctx.CommandStack.Push(batch);
    }
}
