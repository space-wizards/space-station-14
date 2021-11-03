using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Alert;
using Content.Shared.GameTicking;
using Content.Shared.Input;
using Content.Shared.Physics.Pull;
using Content.Shared.Pulling.Components;
using Content.Shared.Pulling.Events;
using Content.Shared.Rotatable;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Players;
using Robust.Shared.IoC;
using Content.Shared.Verbs;
using Robust.Shared.Localization;

namespace Content.Shared.Pulling
{
    [UsedImplicitly]
    public abstract partial class SharedPullingSystem : EntitySystem
    {
        [Dependency] private readonly SharedPullingStateManagementSystem _pullSm = default!;

        /// <summary>
        ///     A mapping of pullers to the entity that they are pulling.
        /// </summary>
        private readonly Dictionary<IEntity, IEntity> _pullers =
            new();

        private readonly HashSet<SharedPullableComponent> _moving = new();
        private readonly HashSet<SharedPullableComponent> _stoppedMoving = new();

        /// <summary>
        ///     If distance between puller and pulled entity lower that this threshold,
        ///     pulled entity will not change its rotation.
        ///     Helps with small distance jittering
        /// </summary>
        private const float ThresholdRotDistance = 1;

        /// <summary>
        ///     If difference between puller and pulled angle  lower that this threshold,
        ///     pulled entity will not change its rotation.
        ///     Helps with diagonal movement jittering
        ///     As of further adjustments, should divide cleanly into 90 degrees
        /// </summary>
        private const float ThresholdRotAngle = 22.5f;

        public IReadOnlySet<SharedPullableComponent> Moving => _moving;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
            SubscribeLocalEvent<PullStartedMessage>(OnPullStarted);
            SubscribeLocalEvent<PullStoppedMessage>(OnPullStopped);
            SubscribeLocalEvent<MoveEvent>(PullerMoved);
            SubscribeLocalEvent<EntInsertedIntoContainerMessage>(HandleContainerInsert);

            SubscribeLocalEvent<SharedPullableComponent, PullStartedMessage>(PullableHandlePullStarted);
            SubscribeLocalEvent<SharedPullableComponent, PullStoppedMessage>(PullableHandlePullStopped);

            SubscribeLocalEvent<SharedPullableComponent, GetOtherVerbsEvent>(AddPullVerbs);

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.MovePulledObject, new PointerInputCmdHandler(HandleMovePulledObject))
                .Register<SharedPullingSystem>();
        }

        private void AddPullVerbs(EntityUid uid, SharedPullableComponent component, GetOtherVerbsEvent args)
        {
            if (args.Hands == null || !args.CanAccess || !args.CanInteract)
                return;

            // Are they trying to pull themselves up by their bootstraps?
            if (args.User == args.Target)
                return;

            //TODO VERB ICONS add pulling icon
            if (component.Puller == args.User)
            {
                Verb verb = new();
                verb.Text = Loc.GetString("pulling-verb-get-data-text-stop-pulling");
                verb.Act = () => TryStopPull(component, args.User);
                args.Verbs.Add(verb);
            }
            else if (CanPull(args.User, args.Target))
            {
                Verb verb = new();
                verb.Text = Loc.GetString("pulling-verb-get-data-text");
                verb.Act = () => TryStartPull(args.User, args.Target);
                args.Verbs.Add(verb);
            }
        }

        // Raise a "you are being pulled" alert if the pulled entity has alerts.
        private static void PullableHandlePullStarted(EntityUid uid, SharedPullableComponent component, PullStartedMessage args)
        {
            if (args.Pulled.Owner.Uid != uid)
                return;

            if (component.Owner.TryGetComponent(out SharedAlertsComponent? alerts))
                alerts.ShowAlert(AlertType.Pulled);
        }

        private static void PullableHandlePullStopped(EntityUid uid, SharedPullableComponent component, PullStoppedMessage args)
        {
            if (args.Pulled.Owner.Uid != uid)
                return;

            if (component.Owner.TryGetComponent(out SharedAlertsComponent? alerts))
                alerts.ClearAlert(AlertType.Pulled);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            _moving.ExceptWith(_stoppedMoving);
            _stoppedMoving.Clear();
        }

        public void Reset(RoundRestartCleanupEvent ev)
        {
            _pullers.Clear();
            _moving.Clear();
            _stoppedMoving.Clear();
        }

        private void OnPullStarted(PullStartedMessage message)
        {
            SetPuller(message.Puller.Owner, message.Pulled.Owner);
        }

        private void OnPullStopped(PullStoppedMessage message)
        {
            RemovePuller(message.Puller.Owner);
        }

        protected void OnPullableMove(EntityUid uid, SharedPullableComponent component, PullableMoveMessage args)
        {
            _moving.Add(component);
        }

        protected void OnPullableStopMove(EntityUid uid, SharedPullableComponent component, PullableStopMovingMessage args)
        {
            _stoppedMoving.Add(component);
        }

        private void PullerMoved(ref MoveEvent ev)
        {
            var puller = ev.Sender;

            if (!TryGetPulled(ev.Sender, out var pulled))
            {
                return;
            }

            // The pulled object may have already been deleted.
            // TODO: Work out why. Monkey + meat spike is a good test for this,
            //  assuming you're still pulling the monkey when it gets gibbed.
            if (pulled.Deleted)
            {
                return;
            }

            if (!pulled.TryGetComponent(out IPhysBody? physics))
            {
                return;
            }

            UpdatePulledRotation(puller, pulled);

            physics.WakeBody();
        }

        // TODO: When Joint networking is less shitcodey fix this to use a dedicated joints message.
        private void HandleContainerInsert(EntInsertedIntoContainerMessage message)
        {
            if (message.Entity.TryGetComponent(out SharedPullableComponent? pullable))
            {
                TryStopPull(pullable);
            }

            if (message.Entity.TryGetComponent(out SharedPullerComponent? puller))
            {
                if (puller.Pulling == null) return;

                if (!puller.Pulling.TryGetComponent(out SharedPullableComponent? pulling))
                {
                    return;
                }

                TryStopPull(pulling);
            }
        }

        private bool HandleMovePulledObject(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
        {
            var player = session?.AttachedEntity;

            if (player == null)
            {
                return false;
            }

            if (!TryGetPulled(player, out var pulled))
            {
                return false;
            }

            if (!pulled.TryGetComponent(out SharedPullableComponent? pullable))
            {
                return false;
            }

            TryMoveTo(pullable, coords);

            return false;
        }

        private void SetPuller(IEntity puller, IEntity pulled)
        {
            _pullers[puller] = pulled;
        }

        private bool RemovePuller(IEntity puller)
        {
            return _pullers.Remove(puller);
        }

        public IEntity? GetPulled(IEntity by)
        {
            return _pullers.GetValueOrDefault(by);
        }

        public bool TryGetPulled(IEntity by, [NotNullWhen(true)] out IEntity? pulled)
        {
            return (pulled = GetPulled(by)) != null;
        }

        public bool IsPulling(IEntity puller)
        {
            return _pullers.ContainsKey(puller);
        }

        private void UpdatePulledRotation(IEntity puller, IEntity pulled)
        {
            // TODO: update once ComponentReference works with directed event bus.
            if (!pulled.TryGetComponent(out RotatableComponent? rotatable))
                return;

            if (!rotatable.RotateWhilePulling)
                return;

            var dir = puller.Transform.WorldPosition - pulled.Transform.WorldPosition;
            if (dir.LengthSquared > ThresholdRotDistance * ThresholdRotDistance)
            {
                var oldAngle = pulled.Transform.WorldRotation;
                var newAngle = Angle.FromWorldVec(dir);

                var diff = newAngle - oldAngle;
                if (Math.Abs(diff.Degrees) > (ThresholdRotAngle / 2f))
                {
                    // Ok, so this bit is difficult because ideally it would look like it's snapping to sane angles.
                    // Otherwise PIANO DOOR STUCK! happens.
                    // But it also needs to work with station rotation / align to the local parent.
                    // So...
                    var baseRotation = pulled.Transform.Parent?.WorldRotation ?? 0f;
                    var localRotation = newAngle - baseRotation;
                    var localRotationSnapped = Angle.FromDegrees(Math.Floor((localRotation.Degrees / ThresholdRotAngle) + 0.5f) * ThresholdRotAngle);
                    pulled.Transform.LocalRotation = localRotationSnapped;
                }
            }
        }
    }
}
