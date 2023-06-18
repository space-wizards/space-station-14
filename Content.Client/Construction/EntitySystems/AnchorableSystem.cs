using Content.Client.Pulling;

using Content.Shared.Construction.Components;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Database;
using Content.Shared.Pulling.Components;
using Content.Shared.Tools.Components;
using Content.Shared.Tools;

using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;

namespace Content.Client.Construction.EntitySystems;

public sealed class AnchorableSystem : SharedAnchorableSystem {

    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;


    protected override void OnUnanchorComplete(EntityUid uid, AnchorableComponent component, TryUnanchorCompletedEvent args)
    {
        if (args.Cancelled || args.Used is not { } used)
            return;

        var xform = Transform(uid);

        RaiseLocalEvent(uid, new BeforeUnanchoredEvent(args.User, used));
        _transform.Unanchor(uid, xform);
        RaiseLocalEvent(uid, new UserUnanchoredEvent(args.User, used));
    }

    protected override void OnAnchorComplete(EntityUid uid, AnchorableComponent component, TryAnchorCompletedEvent args)
    {
        if (args.Cancelled || args.Used is not { } used)
            return;

        var xform = Transform(uid);
        if (TryComp<PhysicsComponent>(uid, out var anchorBody) &&
            !TileFree(xform.Coordinates, anchorBody))
        {
            return;
        }

        // Snap rotation to cardinal (multiple of 90)
        var rot = xform.LocalRotation;
        xform.LocalRotation = Math.Round(rot / (Math.PI / 2)) * (Math.PI / 2);

        if (TryComp<SharedPullableComponent>(uid, out var pullable) && pullable.Puller != null)
        {
            _pulling.TryStopPull(pullable);
        }

        // TODO: Anchoring snaps rn anyway!
        if (component.Snap)
        {
            _transform.SetCoordinates(uid, xform.Coordinates.SnapToGrid(EntityManager, _mapManager));
        }

        RaiseLocalEvent(uid, new BeforeAnchoredEvent(args.User, used));

        if (!xform.Anchored)
            _transform.AnchorEntity(uid, xform);

        RaiseLocalEvent(uid, new UserAnchoredEvent(args.User, used));
    }

    public override void TryToggleAnchor(EntityUid uid, EntityUid userUid, EntityUid usingUid,
        AnchorableComponent? anchorable = null,
        TransformComponent? transform = null,
        SharedPullableComponent? pullable = null,
        ToolComponent? usingTool = null)
    {
        if (!Resolve(uid, ref transform))
            return;

        if (transform.Anchored)
        {
            TryUnAnchor(uid, userUid, usingUid, anchorable, transform, usingTool);
        }
        else
        {
            TryAnchor(uid, userUid, usingUid, anchorable, transform, pullable, usingTool);
        }
    }

    /// <summary>
    ///     Tries to anchor the entity.
    /// </summary>
    /// <returns>true if anchored, false otherwise</returns>
    protected override void TryAnchor(EntityUid uid, EntityUid userUid, EntityUid usingUid,
        AnchorableComponent? anchorable = null,
        TransformComponent? transform = null,
        SharedPullableComponent? pullable = null,
        ToolComponent? usingTool = null)
    {
        if (!Resolve(uid, ref anchorable, ref transform))
            return;

        // Optional resolves.
        Resolve(uid, ref pullable, false);

        if (!Resolve(usingUid, ref usingTool))
            return;

        if (!Valid(uid, userUid, usingUid, true, anchorable, usingTool))
            return;

        if (TryComp<PhysicsComponent>(uid, out var anchorBody) &&
            !TileFree(transform.Coordinates, anchorBody))
        {
            return;
        }

        _tool.UseTool(usingUid, userUid, uid, anchorable.Delay, usingTool.Qualities, new TryAnchorCompletedEvent());
    }
}
