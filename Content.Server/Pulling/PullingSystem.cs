using Content.Shared.Input;
using Content.Shared.Pulling;
using Content.Shared.Pulling.Components;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Input.Binding;
using Robust.Shared.Players;

namespace Content.Server.Pulling
{
    [UsedImplicitly]
    public class PullingSystem : SharedPullingSystem
    {
        public override void Initialize()
        {
            base.Initialize();

            UpdatesAfter.Add(typeof(PhysicsSystem));

            SubscribeLocalEvent<SharedPullableComponent, PullableMoveMessage>(OnPullableMove);
            SubscribeLocalEvent<SharedPullableComponent, PullableStopMovingMessage>(OnPullableStopMove);

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.ReleasePulledObject, InputCmdHandler.FromDelegate(HandleReleasePulledObject))
                .Register<PullingSystem>();
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

            TryStopPull(pullable);
        }
    }
}
