using Content.Shared.Examine;
using Content.Shared.Construction.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Pulling.Components;
using Content.Shared.Tools;
using Content.Shared.Tools.Components;

using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Construction.EntitySystems;

public abstract class SharedAnchorableSystem : EntitySystem
{

    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnchorableComponent, InteractUsingEvent>(OnInteractUsing,
            before: new[] { typeof(ItemSlotsSystem) }, after: new[] { typeof(SharedConstructionSystem) });
        SubscribeLocalEvent<AnchorableComponent, TryAnchorCompletedEvent>(OnAnchorComplete);
        SubscribeLocalEvent<AnchorableComponent, TryUnanchorCompletedEvent>(OnUnanchorComplete);
        SubscribeLocalEvent<AnchorableComponent, ExaminedEvent>(OnAnchoredExamine);
    }

    // Functions abstract due to the inability to use popups or modify the pull state in shared.
    protected abstract void OnUnanchorComplete(EntityUid uid, AnchorableComponent component, TryUnanchorCompletedEvent args);

    protected abstract void OnAnchorComplete(EntityUid uid, AnchorableComponent component, TryAnchorCompletedEvent args);

    public abstract void TryToggleAnchor(EntityUid uid, EntityUid userUid, EntityUid usingUid,
        AnchorableComponent? anchorable = null,
        TransformComponent? transform = null,
        SharedPullableComponent? pullable = null,
        ToolComponent? usingTool = null);

    /// <summary>
    ///     Tries to anchor the entity.
    /// </summary>
    /// <returns>true if anchored, false otherwise</returns>
    protected abstract void TryAnchor(EntityUid uid, EntityUid userUid, EntityUid usingUid,
        AnchorableComponent? anchorable = null,
        TransformComponent? transform = null,
        SharedPullableComponent? pullable = null,
        ToolComponent? usingTool = null);

    /// <summary>
    ///     Tries to unanchor the entity.
    /// </summary>
    /// <returns>true if unanchored, false otherwise</returns>
    protected void TryUnAnchor(EntityUid uid, EntityUid userUid, EntityUid usingUid,
        AnchorableComponent? anchorable = null,
        TransformComponent? transform = null,
        ToolComponent? usingTool = null)
    {
        if (!Resolve(uid, ref anchorable, ref transform))
            return;

        if (!Resolve(usingUid, ref usingTool))
            return;

        if (!Valid(uid, userUid, usingUid, false))
            return;

        _tool.UseTool(usingUid, userUid, uid, anchorable.Delay, usingTool.Qualities, new TryUnanchorCompletedEvent());
        //does not need to call popup; can be in shared
    }


    private void OnInteractUsing(EntityUid uid, AnchorableComponent anchorable, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        // If the used entity doesn't have a tool, return early.
        if (!TryComp(args.Used, out ToolComponent? usedTool) || !usedTool.Qualities.Contains(anchorable.Tool))
            return;

        args.Handled = true;
        TryToggleAnchor(uid, args.User, args.Used, anchorable, usingTool: usedTool);
    }


    protected void OnAnchoredExamine(EntityUid uid, AnchorableComponent component, ExaminedEvent args)
    {
        var isAnchored = Comp<TransformComponent>(uid).Anchored;
        var messageId = isAnchored ? "examinable-anchored" : "examinable-unanchored";
        args.PushMarkup(Loc.GetString(messageId, ("target", uid)));
    }


    protected bool Valid
    (
        EntityUid uid,
        EntityUid userUid,
        EntityUid usingUid,
        bool anchoring,
        AnchorableComponent? anchorable = null,
        ToolComponent? usingTool = null
    )
    {
        if (!Resolve(uid, ref anchorable))
            return false;

        if (!Resolve(usingUid, ref usingTool))
            return false;

        BaseAnchoredAttemptEvent attempt =
            anchoring ? new AnchorAttemptEvent(userUid, usingUid) : new UnanchorAttemptEvent(userUid, usingUid);

        // Need to cast the event or it will be raised as BaseAnchoredAttemptEvent.
        if (anchoring)
            RaiseLocalEvent(uid, (AnchorAttemptEvent) attempt);
        else
            RaiseLocalEvent(uid, (UnanchorAttemptEvent) attempt);

        anchorable.Delay += attempt.Delay;

        return !attempt.Cancelled;
    }


    protected bool TileFree(EntityCoordinates coordinates, PhysicsComponent anchorBody)
    {
        // Probably ignore CanCollide on the anchoring body?
        var gridUid = coordinates.GetGridUid(EntityManager);

        if (!_mapManager.TryGetGrid(gridUid, out var grid))
            return false;

        var tileIndices = grid.TileIndicesFor(coordinates);
        return TileFree(grid, tileIndices, anchorBody.CollisionLayer, anchorBody.CollisionMask);
    }


    public bool TileFree(MapGridComponent grid, Vector2i gridIndices, int collisionLayer = 0, int collisionMask = 0)
    {
        var enumerator = grid.GetAnchoredEntitiesEnumerator(gridIndices);
        var bodyQuery = GetEntityQuery<PhysicsComponent>();

        while (enumerator.MoveNext(out var ent))
        {
            if (!bodyQuery.TryGetComponent(ent, out var body) ||
                !body.CanCollide ||
                !body.Hard)
            {
                continue;
            }

            if ((body.CollisionMask & collisionLayer) != 0x0 ||
                (body.CollisionLayer & collisionMask) != 0x0)
            {
                return false;
            }
        }

        return true;
    }




    [Serializable, NetSerializable]
    protected sealed class TryUnanchorCompletedEvent : SimpleDoAfterEvent
    {
    }

    [Serializable, NetSerializable]
    protected sealed class TryAnchorCompletedEvent : SimpleDoAfterEvent
    {
    }
}
