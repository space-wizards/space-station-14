using Content.Server.Administration.Logs;
using Content.Server.Popups;
using Content.Server.Pulling;

using Content.Shared.Construction.Components;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Pulling.Components;
using Content.Shared.Tools.Components;
using Content.Shared.Tools;

using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;



namespace Content.Server.Construction
{
    public sealed class AnchorableSystem : SharedAnchorableSystem
    {
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly SharedToolSystem _tool = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly PullingSystem _pulling = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;


        //keep for popup and admin log
        protected override void OnUnanchorComplete(EntityUid uid, AnchorableComponent component, TryUnanchorCompletedEvent args)
        {
            if (args.Cancelled || args.Used is not { } used)
                return;

            var xform = Transform(uid);

            RaiseLocalEvent(uid, new BeforeUnanchoredEvent(args.User, used));
            _transform.Unanchor(uid, xform);
            RaiseLocalEvent(uid, new UserUnanchoredEvent(args.User, used));

            _popup.PopupEntity(Loc.GetString("anchorable-unanchored"), uid);

            _adminLogger.Add(
                LogType.Unanchor,
                LogImpact.Low,
                $"{EntityManager.ToPrettyString(args.User):user} unanchored {EntityManager.ToPrettyString(uid):anchored} using {EntityManager.ToPrettyString(used):using}"
            );
        }

        protected override void OnAnchorComplete(EntityUid uid, AnchorableComponent component, TryAnchorCompletedEvent args)
        {
            if (args.Cancelled || args.Used is not { } used)
                return;

            var xform = Transform(uid);
            if (TryComp<PhysicsComponent>(uid, out var anchorBody) &&
                !TileFree(xform.Coordinates, anchorBody))
            {
                _popup.PopupEntity(Loc.GetString("anchorable-occupied"), uid, args.User);
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

            _popup.PopupEntity(Loc.GetString("anchorable-anchored"), uid);

            _adminLogger.Add(
                LogType.Anchor,
                LogImpact.Low,
                $"{EntityManager.ToPrettyString(args.User):user} anchored {EntityManager.ToPrettyString(uid):anchored} using {EntityManager.ToPrettyString(used):using}"
            );
        }


        /// <summary>
        ///     Tries to toggle the anchored status of this component's owner.
        ///     override is used due to popup and adminlog being server side systems in this case.
        /// </summary>
        /// <returns>true if toggled, false otherwise</returns>
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

                // Log unanchor attempt (server only)
                _adminLogger.Add(LogType.Anchor, LogImpact.Low, $"{ToPrettyString(userUid):user} is trying to unanchor {ToPrettyString(uid):entity} from {transform.Coordinates:targetlocation}");
            }
            else
            {
                TryAnchor(uid, userUid, usingUid, anchorable, transform, pullable, usingTool);

                // Log anchor attempt (server only)
                _adminLogger.Add(LogType.Anchor, LogImpact.Low, $"{ToPrettyString(userUid):user} is trying to anchor {ToPrettyString(uid):entity} to {transform.Coordinates:targetlocation}");
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
                _popup.PopupEntity(Loc.GetString("anchorable-occupied"), uid, userUid);
                return;
            }

            _tool.UseTool(usingUid, userUid, uid, anchorable.Delay, usingTool.Qualities, new TryAnchorCompletedEvent());
        }
    }
}
