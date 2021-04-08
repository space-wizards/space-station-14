#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.GameObjects.Components.Pulling;
using Content.Shared.GameObjects.EntitySystemMessages.Pulling;
using Content.Shared.GameTicking;
using Content.Shared.Input;
using Content.Shared.Physics.Pull;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Players;

namespace Content.Shared.GameObjects.EntitySystems
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
            if (!TryGetPulled(ev.Sender, out var pulled))
            {
                return;
            }

            if (!pulled.TryGetComponent(out IPhysBody? physics))
            {
                return;
            }

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
    }
}
