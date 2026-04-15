using Content.MapEditor.Commands;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.MapEditor.Tools;

/// <summary>
///     Spawns the selected entity prototype at the clicked tile position on the active grid.
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

        // GridTileToLocal already returns tile center (adds TileSizeHalfVector).
        var coords = ctx.MapSystem.GridTileToLocal(gridUid, grid, tilePos);

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
