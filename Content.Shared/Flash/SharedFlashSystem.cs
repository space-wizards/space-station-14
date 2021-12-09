using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Shared.Flash
{
    public abstract class SharedFlashSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedFlashableComponent, ComponentGetState>(OnFlashableGetState);
            SubscribeLocalEvent<SharedFlashableComponent, ComponentGetStateAttemptEvent>(OnGetStateAttempt);
        }

        private void OnGetStateAttempt(EntityUid uid, SharedFlashableComponent component, ComponentGetStateAttemptEvent args)
        {
            // Only send state to the player attached to the entity.
            if (args.Player.AttachedEntity != uid)
                args.Cancel();
        }

        private void OnFlashableGetState(EntityUid uid, SharedFlashableComponent component, ref ComponentGetState args)
        {
            args.State = new FlashableComponentState(component.Duration, component.LastFlash);
        }
    }
}
