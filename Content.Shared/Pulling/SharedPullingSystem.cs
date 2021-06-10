#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.GameTicking;
using Content.Shared.Input;
using Content.Shared.Physics.Pull;
using Content.Shared.Pulling.Components;
using Content.Shared.Rotatable;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Players;

namespace Content.Shared.Pulling
{
    [UsedImplicitly]
    public abstract class SharedPullingSystem : EntitySystem, IResettingEntitySystem
    {
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
        /// </summary>
        private const float ThresholdRotAngle = 30;

        public IReadOnlySet<SharedPullableComponent> Moving => _moving;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PullStartedMessage>(OnPullStarted);
            SubscribeLocalEvent<PullStoppedMessage>(OnPullStopped);
            SubscribeLocalEvent<MoveEvent>(PullerMoved);
            SubscribeLocalEvent<EntInsertedIntoContainerMessage>(HandleContainerInsert);

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.MovePulledObject, new PointerInputCmdHandler(HandleMovePulledObject))
                .Bind(ContentKeyFunctions.ReleasePulledObject, InputCmdHandler.FromDelegate(HandleReleasePulledObject))
                .Register<SharedPullingSystem>();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            _moving.ExceptWith(_stoppedMoving);
            _stoppedMoving.Clear();
        }

        public void Reset()
        {
            _pullers.Clear();
            _moving.Clear();
            _stoppedMoving.Clear();
        }

        private void OnPullStarted(PullStartedMessage message)
        {
            if (_pullers.TryGetValue(message.Puller.Owner, out var pulled) &&
                pulled.TryGetComponent(out SharedPullableComponent? pulledComponent))
            {
                pulledComponent.TryStopPull();
            }

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

        private void PullerMoved(MoveEvent ev)
        {
            var puller = ev.Sender;
            if (!TryGetPulled(ev.Sender, out var pulled))
            {
                return;
            }

            if (!pulled.TryGetComponent(out IPhysBody? physics))
            {
                return;
            }

            UpdatePulledRotation(puller, pulled);

            physics.WakeBody();

            if (pulled.TryGetComponent(out SharedPullableComponent? pullable))
            {
                pullable.MovingTo = null;
            }
        }

        // TODO: When Joint networking is less shitcodey fix this to use a dedicated joints message.
        private void HandleContainerInsert(EntInsertedIntoContainerMessage message)
        {
            if (message.Entity.TryGetComponent(out SharedPullableComponent? pullable))
            {
                pullable.TryStopPull();
            }

            if (message.Entity.TryGetComponent(out SharedPullerComponent? puller))
            {
                if (puller.Pulling == null) return;

                if (!puller.Pulling.TryGetComponent(out SharedPullableComponent? pulling))
                {
                    return;
                }

                pulling.TryStopPull();
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

            pullable.TryMoveTo(coords.ToMap(EntityManager));

            return false;
        }

        private void HandleReleasePulledObject(ICommonSession? session)
        {
            var player = session?.AttachedEntity;

            if (player == null)
            {
                return;
            }

            if (!TryGetPulled(player, out var pulled))
            {
                return;
            }

            if (!pulled.TryGetComponent(out SharedPullableComponent? pullable))
            {
                return;
            }

            pullable.TryStopPull();
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
            if (!pulled.TryGetComponent(out SharedRotatableComponent? rotatable))
                return;

            if (!rotatable.RotateWhilePulling)
                return;

            var dir = puller.Transform.WorldPosition - pulled.Transform.WorldPosition;
            if (dir.LengthSquared > ThresholdRotDistance * ThresholdRotDistance)
            {
                var oldAngle = pulled.Transform.WorldRotation;
                var newAngle = Angle.FromWorldVec(dir);

                var diff = newAngle - oldAngle;
                if (Math.Abs(diff.Degrees) > ThresholdRotAngle)
                    pulled.Transform.WorldRotation = newAngle;
            }
        }
    }
}
