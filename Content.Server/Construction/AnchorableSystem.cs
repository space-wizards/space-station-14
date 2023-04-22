using Content.Server.Administration.Logs;
using Content.Server.Coordinates.Helpers;
using Content.Server.Popups;
using Content.Server.Pulling;
using Content.Shared.Construction.Components;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Pulling.Components;
using Content.Shared.Tools;
using Content.Shared.Tools.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;

namespace Content.Server.Construction
{
    public sealed class AnchorableSystem : SharedAnchorableSystem
    {
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly SharedToolSystem _tool = default!;
        [Dependency] private readonly PullingSystem _pulling = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<AnchorableComponent, TryAnchorCompletedEvent>(OnAnchorComplete);
            SubscribeLocalEvent<AnchorableComponent, TryUnanchorCompletedEvent>(OnUnanchorComplete);
            SubscribeLocalEvent<AnchorableComponent, ExaminedEvent>(OnAnchoredExamine);
        }

        private void OnAnchoredExamine(EntityUid uid, AnchorableComponent component, ExaminedEvent args)
        {
            var isAnchored = Comp<TransformComponent>(uid).Anchored;
            var messageId = isAnchored ? "examinable-anchored" : "examinable-unanchored";
            args.PushMarkup(Loc.GetString(messageId, ("target", uid)));
        }

        private void OnUnanchorComplete(EntityUid uid, AnchorableComponent component, TryUnanchorCompletedEvent args)
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

        private void OnAnchorComplete(EntityUid uid, AnchorableComponent component, TryAnchorCompletedEvent args)
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

        private bool TileFree(EntityCoordinates coordinates, PhysicsComponent anchorBody)
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

        /// <summary>
        ///     Checks if a tool can change the anchored status.
        /// </summary>
        /// <returns>true if it is valid, false otherwise</returns>
        private bool Valid(EntityUid uid, EntityUid userUid, EntityUid usingUid, bool anchoring, AnchorableComponent? anchorable = null, ToolComponent? usingTool = null)
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
                _popup.PopupEntity(Loc.GetString("anchorable-occupied"), uid, userUid);
                return;
            }

            _tool.UseTool(usingUid, userUid, uid, anchorable.Delay, usingTool.Qualities, new TryAnchorCompletedEvent());
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
            if (!Resolve(uid, ref anchorable, ref transform))
                return;

            if (!Resolve(usingUid, ref usingTool))
                return;

            if (!Valid(uid, userUid, usingUid, false))
                return;

            _tool.UseTool(usingUid, userUid, uid, anchorable.Delay, usingTool.Qualities, new TryUnanchorCompletedEvent());
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

                // Log unanchor attempt
                _adminLogger.Add(LogType.Anchor, LogImpact.Low, $"{ToPrettyString(userUid):user} is trying to unanchor {ToPrettyString(uid):entity} from {transform.Coordinates:targetlocation}");
            }
            else
            {
                TryAnchor(uid, userUid, usingUid, anchorable, transform, pullable, usingTool);

                // Log anchor attempt
                _adminLogger.Add(LogType.Anchor, LogImpact.Low, $"{ToPrettyString(userUid):user} is trying to anchor {ToPrettyString(uid):entity} to {transform.Coordinates:targetlocation}");
            }
        }
    }
}
