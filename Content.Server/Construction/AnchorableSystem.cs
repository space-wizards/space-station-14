using System.Threading;
using Content.Server.Administration.Logs;
using Content.Server.Coordinates.Helpers;
using Content.Server.Popups;
using Content.Server.Pulling;
using Content.Server.Tools;
using Content.Shared.Construction.Components;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Pulling.Components;
using Content.Shared.Tools.Components;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;

namespace Content.Server.Construction
{
    public sealed class AnchorableSystem : SharedAnchorableSystem
    {
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly ToolSystem _tool = default!;
        [Dependency] private readonly PullingSystem _pulling = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<AnchorableComponent, TryAnchorCompletedEvent>(OnAnchorComplete);
            SubscribeLocalEvent<AnchorableComponent, TryAnchorCancelledEvent>(OnAnchorCancelled);
            SubscribeLocalEvent<AnchorableComponent, TryUnanchorCompletedEvent>(OnUnanchorComplete);
            SubscribeLocalEvent<AnchorableComponent, TryUnanchorCancelledEvent>(OnUnanchorCancelled);
            SubscribeLocalEvent<AnchorableComponent, ExaminedEvent>(OnAnchoredExamine);
        }

        private void OnAnchoredExamine(EntityUid uid, AnchorableComponent component, ExaminedEvent args)
        {
            var isAnchored = Comp<TransformComponent>(uid).Anchored;
            var messageId = isAnchored ? "examinable-anchored" : "examinable-unanchored";
            args.PushMarkup(Loc.GetString(messageId, ("target", uid)));
        }

        private void OnUnanchorCancelled(EntityUid uid, AnchorableComponent component, TryUnanchorCancelledEvent args)
        {
            component.CancelToken = null;
        }

        private void OnUnanchorComplete(EntityUid uid, AnchorableComponent component, TryUnanchorCompletedEvent args)
        {
            component.CancelToken = null;
            var xform = Transform(uid);

            RaiseLocalEvent(uid, new BeforeUnanchoredEvent(args.User, args.Using));
            xform.Anchored = false;
            RaiseLocalEvent(uid, new UserUnanchoredEvent(args.User, args.Using));

            _popup.PopupEntity(Loc.GetString("anchorable-unanchored"), uid, Filter.Pvs(uid, entityManager: EntityManager));

            _adminLogger.Add(
                LogType.Unanchor,
                LogImpact.Low,
                $"{EntityManager.ToPrettyString(args.User):user} unanchored {EntityManager.ToPrettyString(uid):anchored} using {EntityManager.ToPrettyString(args.Using):using}"
            );
        }

        private void OnAnchorCancelled(EntityUid uid, AnchorableComponent component, TryAnchorCancelledEvent args)
        {
            component.CancelToken = null;
        }

        private void OnAnchorComplete(EntityUid uid, AnchorableComponent component, TryAnchorCompletedEvent args)
        {
            component.CancelToken = null;
            var xform = Transform(uid);
            if (TryComp<PhysicsComponent>(uid, out var anchorBody) &&
                !TileFree(xform.Coordinates, anchorBody))
            {
                _popup.PopupEntity(Loc.GetString("anchorable-occupied"), uid, Filter.Entities(args.User));
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
                xform.Coordinates = xform.Coordinates.SnapToGrid(EntityManager, _mapManager);

            RaiseLocalEvent(uid, new BeforeAnchoredEvent(args.User, args.Using));
            xform.Anchored = true;
            RaiseLocalEvent(uid, new UserAnchoredEvent(args.User, args.Using));

            _popup.PopupEntity(Loc.GetString("anchorable-anchored"), uid, Filter.Pvs(uid, entityManager: EntityManager));

            _adminLogger.Add(
                LogType.Anchor,
                LogImpact.Low,
                $"{EntityManager.ToPrettyString(args.User):user} anchored {EntityManager.ToPrettyString(uid):anchored} using {EntityManager.ToPrettyString(args.Using):using}"
            );
        }

        private bool TileFree(EntityCoordinates coordinates, PhysicsComponent anchorBody)
        {
            // Probably ignore CanCollide on the anchoring body?
            var gridUid = coordinates.GetGridUid(EntityManager);

            if (!_mapManager.TryGetGrid(gridUid, out var grid))
                return false;

            var tileIndices = grid.TileIndicesFor(coordinates);
            var enumerator = grid.GetAnchoredEntitiesEnumerator(tileIndices);
            var bodyQuery = GetEntityQuery<PhysicsComponent>();

            while (enumerator.MoveNext(out var ent))
            {
                if (!bodyQuery.TryGetComponent(ent, out var body) ||
                    !body.CanCollide ||
                    !body.Hard)
                {
                    continue;
                }

                if ((body.CollisionMask & anchorBody.CollisionLayer) != 0x0 ||
                    (body.CollisionLayer & anchorBody.CollisionMask) != 0x0)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     Checks if a tool can change the anchored status.
        /// </summary>
        /// <returns>true if it is valid, false otherwise</returns>
        private bool Valid(EntityUid uid, EntityUid userUid, EntityUid usingUid, bool anchoring, AnchorableComponent? anchorable = null, ToolComponent? usingTool = null)
        {
            if (!Resolve(uid, ref anchorable) ||
                anchorable.CancelToken != null)
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

        /// <summary>
        ///     Tries to anchor the entity.
        /// </summary>
        /// <returns>true if anchored, false otherwise</returns>
        private void TryAnchor(EntityUid uid, EntityUid userUid, EntityUid usingUid,
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
                _popup.PopupEntity(Loc.GetString("anchorable-occupied"), uid, Filter.Entities(userUid));
                return;
            }

            anchorable.CancelToken = new CancellationTokenSource();

            _tool.UseTool(usingUid, userUid, uid, 0f, anchorable.Delay, usingTool.Qualities,
                new TryAnchorCompletedEvent(userUid, usingUid), new TryAnchorCancelledEvent(userUid, usingUid), uid, cancelToken: anchorable.CancelToken.Token);
        }

        /// <summary>
        ///     Tries to unanchor the entity.
        /// </summary>
        /// <returns>true if unanchored, false otherwise</returns>
        private void TryUnAnchor(EntityUid uid, EntityUid userUid, EntityUid usingUid,
            AnchorableComponent? anchorable = null,
            TransformComponent? transform = null,
            ToolComponent? usingTool = null)
        {
            if (!Resolve(uid, ref anchorable, ref transform) ||
                anchorable.CancelToken != null)
                return;

            if (!Resolve(usingUid, ref usingTool)) return;

            if (!Valid(uid, userUid, usingUid, false)) return;

            anchorable.CancelToken = new CancellationTokenSource();

            _tool.UseTool(usingUid, userUid, uid, 0f, anchorable.Delay, usingTool.Qualities,
                new TryUnanchorCompletedEvent(userUid, usingUid), new TryUnanchorCancelledEvent(userUid, usingUid), uid, cancelToken: anchorable.CancelToken.Token);
        }

        /// <summary>
        ///     Tries to toggle the anchored status of this component's owner.
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
            }
            else
            {
                TryAnchor(uid, userUid, usingUid, anchorable, transform, pullable, usingTool);
            }
        }

        private abstract class AnchorEvent : EntityEventArgs
        {
            public EntityUid User;
            public EntityUid Using;

            protected AnchorEvent(EntityUid userUid, EntityUid usingUid)
            {
                User = userUid;
                Using = usingUid;
            }
        }

        private sealed class TryUnanchorCompletedEvent : AnchorEvent
        {
            public TryUnanchorCompletedEvent(EntityUid userUid, EntityUid usingUid) : base(userUid, usingUid)
            {
            }
        }

        private sealed class TryUnanchorCancelledEvent : AnchorEvent
        {
            public TryUnanchorCancelledEvent(EntityUid userUid, EntityUid usingUid) : base(userUid, usingUid)
            {
            }
        }

        private sealed class TryAnchorCompletedEvent : AnchorEvent
        {
            public TryAnchorCompletedEvent(EntityUid userUid, EntityUid usingUid) : base(userUid, usingUid)
            {
            }
        }

        private sealed class TryAnchorCancelledEvent : AnchorEvent
        {
            public TryAnchorCancelledEvent(EntityUid userUid, EntityUid usingUid) : base(userUid, usingUid)
            {
            }
        }
    }
}
