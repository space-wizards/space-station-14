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
        }

        private void OnFlashableGetState(EntityUid uid, SharedFlashableComponent component, ref ComponentGetState args)
        {
            // Only send state to the player attached to the entity.
            if (args.Player.AttachedEntityUid != uid)
                return;

            args.State = new FlashableComponentState(component.Duration, component.LastFlash);
        }
    }
}
