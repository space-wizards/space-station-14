using Content.Shared.Pulling;
using JetBrains.Annotations;
using Robust.Server.GameObjects;

namespace Content.Server.Pulling
{
    [UsedImplicitly]
    public class PullingSystem : SharedPullingSystem
    {
        public override void Initialize()
        {
            base.Initialize();

            UpdatesAfter.Add(typeof(PhysicsSystem));

            SubscribeLocalEvent<PullableComponent, PullableMoveMessage>(OnPullableMove);
            SubscribeLocalEvent<PullableComponent, PullableStopMovingMessage>(OnPullableStopMove);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            UnsubscribeLocalEvent<PullableComponent, PullableMoveMessage>(OnPullableMove);
            UnsubscribeLocalEvent<PullableComponent, PullableStopMovingMessage>(OnPullableStopMove);
        }
    }
}
