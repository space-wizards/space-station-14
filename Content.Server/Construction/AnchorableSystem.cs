using System;
using System.Threading.Tasks;
using Content.Server.Construction.Components;
using Content.Server.Coordinates.Helpers;
using Content.Server.Pulling;
using Content.Server.Tools;
using Content.Server.Tools.Components;
using Content.Shared.Interaction;
using Content.Shared.Pulling.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Construction
{
    public class AnchorableSystem : EntitySystem
    {
        [Dependency] private readonly ToolSystem _toolSystem = default!;
        [Dependency] private readonly PullingSystem _pullingSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<AnchorableComponent, InteractUsingEvent>(OnInteractUsing, after:new []{typeof(ConstructionSystem)});
        }

        private async void OnInteractUsing(EntityUid uid, AnchorableComponent anchorable, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            // If the used entity doesn't have a tool, return early.
            if (!EntityManager.TryGetComponent(args.Used, out ToolComponent? usedTool))
                return;

            args.Handled = await TryToggleAnchor(uid, args.User, args.Used, anchorable, usingTool:usedTool);
        }

        /// <summary>
        ///     Checks if a tool can change the anchored status.
        /// </summary>
        /// <returns>true if it is valid, false otherwise</returns>
        private async Task<bool> Valid(EntityUid uid, EntityUid userUid, EntityUid usingUid, bool anchoring, AnchorableComponent? anchorable = null, ToolComponent? usingTool = null)
        {
            if (!Resolve(uid, ref anchorable))
                return false;

            if (!Resolve(usingUid, ref usingTool))
                return false;

            BaseAnchoredAttemptEvent attempt =
                anchoring ? new AnchorAttemptEvent(userUid, usingUid) : new UnanchorAttemptEvent(userUid, usingUid);

            // Need to cast the event or it will be raised as BaseAnchoredAttemptEvent.
            if (anchoring)
                RaiseLocalEvent(uid, (AnchorAttemptEvent) attempt, false);
            else
                RaiseLocalEvent(uid, (UnanchorAttemptEvent) attempt, false);

            if (attempt.Cancelled)
                return false;

            return await _toolSystem.UseTool(usingUid, userUid, uid, 0f, 0.5f + attempt.Delay, anchorable.Tool, toolComponent:usingTool);
        }

        /// <summary>
        ///     Tries to anchor the entity.
        /// </summary>
        /// <returns>true if anchored, false otherwise</returns>
        public async Task<bool> TryAnchor(EntityUid uid, EntityUid userUid, EntityUid usingUid,
            AnchorableComponent? anchorable = null,
            TransformComponent? transform = null,
            SharedPullableComponent? pullable = null,
            ToolComponent? usingTool = null)
        {
            if (!Resolve(uid, ref anchorable, ref transform))
                return false;

            // Optional resolves.
            Resolve(uid, ref pullable, false);

            if (!Resolve(usingUid, ref usingTool))
                return false;

            if (!(await Valid(uid, userUid, usingUid, true, anchorable, usingTool)))
            {
                return false;
            }

            // Snap rotation to cardinal (multiple of 90)
            var rot = transform.LocalRotation;
            transform.LocalRotation = Math.Round(rot / (Math.PI / 2)) * (Math.PI / 2);

            if (pullable is { Puller: {} })
            {
                _pullingSystem.TryStopPull(pullable);
            }

            if (anchorable.Snap)
                transform.Coordinates = transform.Coordinates.SnapToGrid();

            RaiseLocalEvent(uid, new BeforeAnchoredEvent(userUid, usingUid), false);

            transform.Anchored = true;

            RaiseLocalEvent(uid, new UserAnchoredEvent(userUid, usingUid), false);

            return true;
        }

        /// <summary>
        ///     Tries to unanchor the entity.
        /// </summary>
        /// <returns>true if unanchored, false otherwise</returns>
        public async Task<bool> TryUnAnchor(EntityUid uid, EntityUid userUid, EntityUid usingUid,
            AnchorableComponent? anchorable = null,
            TransformComponent? transform = null,
            ToolComponent? usingTool = null)
        {
            if (!Resolve(uid, ref anchorable, ref transform))
                return false;

            if (!Resolve(usingUid, ref usingTool))
                return false;

            if (!(await Valid(uid, userUid, usingUid, false)))
            {
                return false;
            }

            RaiseLocalEvent(uid, new BeforeUnanchoredEvent(userUid, usingUid), false);

            transform.Anchored = false;

            RaiseLocalEvent(uid, new UserUnanchoredEvent(userUid, usingUid), false);

            return true;
        }

        /// <summary>
        ///     Tries to toggle the anchored status of this component's owner.
        /// </summary>
        /// <returns>true if toggled, false otherwise</returns>
        public async Task<bool> TryToggleAnchor(EntityUid uid, EntityUid userUid, EntityUid usingUid,
            AnchorableComponent? anchorable = null,
            TransformComponent? transform = null,
            SharedPullableComponent? pullable = null,
            ToolComponent? usingTool = null)
        {
            if (!Resolve(uid, ref transform))
                return false;

            return transform.Anchored ?
                await TryUnAnchor(uid, userUid, usingUid, anchorable, transform, usingTool) :
                await TryAnchor(uid, userUid, usingUid, anchorable, transform, pullable, usingTool);
        }
    }
}
