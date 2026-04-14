using System;
using Content.MapEditor.Commands;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.MapEditor.Tools;

/// <summary>
///     Selects an entity at the clicked tile position. Supports delete and rotation
///     of the selected entity.
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

    public void OnMouseDown(ToolContext ctx, Vector2i tilePos)
    {
        var gridUid = ctx.ActiveGridUid;
        var grid = ctx.EntityManager.GetComponent<MapGridComponent>(gridUid);

        // Find anchored entities at this tile. Take the first one found.
        EntityUid? found = null;
        foreach (var ent in ctx.MapSystem.GetAnchoredEntities(gridUid, grid, tilePos))
        {
            found = ent;
            break;
        }

        if (found != null)
        {
            SelectedEntity = found;
            SelectedTilePos = tilePos;
        }
        else
        {
            // Also check for non-anchored entities at this tile by scanning transforms.
            // For Phase 4 we only check anchored entities.
            SelectedEntity = null;
            SelectedTilePos = null;
        }
    }

    public void OnMouseDrag(ToolContext ctx, Vector2i tilePos) { }
    public void OnMouseUp(ToolContext ctx) { }

    /// <summary>
    ///     Deletes the selected entity and pushes an undo command.
    /// </summary>
    public void DeleteSelected(ToolContext ctx)
    {
        if (SelectedEntity == null || !ctx.EntityManager.EntityExists(SelectedEntity.Value))
            return;

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
