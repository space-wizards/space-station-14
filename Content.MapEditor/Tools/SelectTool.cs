using System;
using Content.MapEditor.Commands;
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

    /// <summary>
    ///     The finalized selection rectangle, or null if nothing is selected.
    ///     Uses inclusive tile coordinates: Bottom-Left is min corner, Top-Right+1 is max corner.
    /// </summary>
    public Box2i? Selection { get; private set; }

    /// <summary>Start corner of the in-progress drag, exposed for overlay preview.</summary>
    public Vector2i? DragStart => _isDragging ? _dragStart : null;

    /// <summary>Current end corner of the in-progress drag, exposed for overlay preview.</summary>
    public Vector2i? DragEnd => _isDragging ? _dragEnd : null;

    public void OnMouseDown(ToolContext ctx, Vector2i tilePos)
    {
        _isDragging = true;
        _dragStart = tilePos;
        _dragEnd = tilePos;
        Selection = null; // clear previous selection on new drag
    }

    public void OnMouseDrag(ToolContext ctx, Vector2i tilePos)
    {
        _dragEnd = tilePos;
    }

    public void OnMouseUp(ToolContext ctx)
    {
        if (!_isDragging)
            return;

        _isDragging = false;

        // Finalize selection as a Box2i (tiles are inclusive, so +1 on max).
        var minX = Math.Min(_dragStart.X, _dragEnd.X);
        var minY = Math.Min(_dragStart.Y, _dragEnd.Y);
        var maxX = Math.Max(_dragStart.X, _dragEnd.X);
        var maxY = Math.Max(_dragStart.Y, _dragEnd.Y);
        Selection = new Box2i(minX, minY, maxX + 1, maxY + 1);
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
