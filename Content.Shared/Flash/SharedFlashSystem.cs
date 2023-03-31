using Robust.Shared.GameStates;

namespace Content.Shared.Flash
{
    public abstract class SharedFlashSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<FlashableComponent, ComponentGetState>(OnFlashableGetState);
        }

        private static void OnFlashableGetState(EntityUid uid, FlashableComponent component, ref ComponentGetState args)
        {
            args.State = new FlashableComponentState(component.Duration, component.LastFlash);
        }
    }
}
