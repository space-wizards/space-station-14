#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.GameObjects.Components.Pulling;
using Content.Shared.Input;
using Content.Shared.Physics.Pull;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Input.Binding;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Players;

namespace Content.Shared.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class SharedPullingSystem : EntitySystem
    {
        /// <summary>
        ///     A mapping of pullers to the entity that they are pulling.
        /// </summary>
        private readonly Dictionary<IEntity, IEntity> _pullers =
            new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PullStartedMessage>(OnPullStarted);
            SubscribeLocalEvent<PullStoppedMessage>(OnPullStopped);
            SubscribeLocalEvent<MoveEvent>(PullerMoved);

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.MovePulledObject, new PointerInputCmdHandler(HandleMovePulledObject))
                .Bind(ContentKeyFunctions.ReleasePulledObject, InputCmdHandler.FromDelegate(HandleReleasePulledObject))
                .Register<SharedPullingSystem>();
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

        private void PullerMoved(MoveEvent ev)
        {
            if (!TryGetPulled(ev.Sender, out var pulled))
            {
                return;
            }

            if (!pulled.TryGetComponent(out IPhysicsComponent? physics))
            {
                return;
            }

            physics.WakeBody();
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

            pullable.TryMoveTo(coords);

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
