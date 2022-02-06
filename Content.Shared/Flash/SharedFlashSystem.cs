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

        private static void OnGetStateAttempt(EntityUid uid, SharedFlashableComponent component, ref ComponentGetStateAttemptEvent args)
        {
            // Only send state to the player attached to the entity.
            if (args.Player.AttachedEntity != uid)
                args.Cancelled = true;
        }

        private static void OnFlashableGetState(EntityUid uid, SharedFlashableComponent component, ref ComponentGetState args)
        {
            args.State = new FlashableComponentState(component.Duration, component.LastFlash);
        }
    }
}
