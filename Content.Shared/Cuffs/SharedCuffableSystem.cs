using Content.Shared.Cuffs.Components;
using Content.Shared.Pulling.Components;
using Robust.Shared.GameObjects;

namespace Content.Shared.Cuffs
{
    public abstract class SharedCuffableSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SharedCuffableComponent, StopPullingEvent>(HandleStopPull);
        }

        private void HandleStopPull(EntityUid uid, SharedCuffableComponent component, StopPullingEvent args)
        {
            if (args.User == null || !EntityManager.TryGetEntity(args.User.Value, out var user)) return;

            if (user == component.Owner && !component.CanStillInteract)
            {
                args.Cancel();
            }
        }
    }
}
