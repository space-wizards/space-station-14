using Content.MapEditor.Commands;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.MapEditor.Tools;

/// <summary>
///     Spawns the selected entity prototype at the clicked position on the active grid.
///     Snaps to tile center by default. Hold Shift for free (non-snapped) placement.
/// </summary>
public sealed class EntityPlaceTool : IEditorTool
{
    public string Name => "Entity Place";

    public void OnMouseDown(ToolContext ctx, Vector2i tilePos)
    {
        if (string.IsNullOrEmpty(ctx.SelectedEntityPrototype))
            return;

        var gridUid = ctx.ActiveGridUid;
        var grid = ctx.EntityManager.GetComponent<MapGridComponent>(gridUid);

        EntityCoordinates coords;
        if (ctx.ShiftHeld)
        {
            // Free placement: use exact cursor world position, convert to grid-local.
            var xformSys = ctx.EntityManager.System<SharedTransformSystem>();
            var invMatrix = xformSys.GetInvWorldMatrix(gridUid);
            var gridLocal = System.Numerics.Vector2.Transform(ctx.CursorWorldPosition, invMatrix);
            coords = new EntityCoordinates(gridUid, gridLocal);
        }
        else
        {
            // Snapped: tile center.
            coords = ctx.MapSystem.GridTileToLocal(gridUid, grid, tilePos);
        }

        var uid = ctx.EntityManager.SpawnEntity(ctx.SelectedEntityPrototype, coords);

        // Apply placement rotation if set.
        if (ctx.PlacementRotation != Angle.Zero)
        {
            var xformSystem = ctx.EntityManager.System<SharedTransformSystem>();
            xformSystem.SetLocalRotation(uid, ctx.PlacementRotation);
        }

        var cmd = new SpawnEntityCommand(ctx.EntityManager, uid);
        ctx.CommandStack.Push(cmd);
    }

    public void OnMouseDrag(ToolContext ctx, Vector2i tilePos) { }
    public void OnMouseUp(ToolContext ctx) { }
}
