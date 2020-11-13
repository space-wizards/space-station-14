#nullable enable
using Content.Shared.Physics.Pull;
using Robust.Client.Player;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;

namespace Content.Client.GameObjects.EntitySystems
{
    public sealed class PullerSystem : EntitySystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PullStartedMessage>(HandlePullStartedMessage);
            SubscribeLocalEvent<PullStoppedMessage>(HandlePullStoppedMessage);
        }

        private void HandlePullStartedMessage(PullStartedMessage message)
        {
            var player = _playerManager?.LocalPlayer?.ControlledEntity;

            if (player == null || !player.TryGetComponent(out IPhysicsComponent? physicsComponent)) return;

            if (message.Puller == physicsComponent)
            {
                message.Pulled.Predict = true;
            }

            if (message.Pulled == physicsComponent)
            {
                message.Puller.Predict = true;
            }
        }

        private void HandlePullStoppedMessage(PullStoppedMessage message)
        {
            var player = _playerManager?.LocalPlayer?.ControlledEntity;

            if (player == null || !player.TryGetComponent(out IPhysicsComponent? physicsComponent)) return;

            if (message.Puller == physicsComponent)
            {
                message.Pulled.Predict = false;
            }

            if (message.Pulled == physicsComponent)
            {
                message.Puller.Predict = false;
            }
        }
    }
}
