using Content.Shared.Pulling;
using JetBrains.Annotations;
using Robust.Client.Physics;

namespace Content.Client.Pulling
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
    }
}
