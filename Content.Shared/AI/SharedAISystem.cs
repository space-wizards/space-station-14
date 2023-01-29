using Content.Shared.Interaction.Events;

namespace Content.Shared.AI
{
    public abstract class SharedAISystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SharedAIComponent, UseAttemptEvent>(OnAttempt);
        }

        private void OnAttempt(EntityUid uid, SharedAIComponent component, CancellableEntityEventArgs args)
        {
            if (!component.isHolopad)
                args.Cancel();
        }
    }
}
