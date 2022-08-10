using Robust.Shared.GameStates;

namespace Content.Shared.Audio
{
    public abstract class SharedAmbientSoundSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<AmbientSoundComponent, ComponentGetState>(GetCompState);
            SubscribeLocalEvent<AmbientSoundComponent, ComponentHandleState>(HandleCompState);
        }

        public void SetAmbience(EntityUid uid, bool value)
        {
            // Reason I didn't make this eventbus for the callers is because it seemed a bit silly
            // trying to account for damageable + powered + toggle, plus we can't just check if it's powered.
            // So we'll just call it directly for whatever.
            if (!EntityManager.TryGetComponent<AmbientSoundComponent>(uid, out var ambience) ||
                ambience.Enabled == value) return;

            ambience.Enabled = value;
            Dirty(ambience);
        }

        private void HandleCompState(EntityUid uid, AmbientSoundComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not AmbientSoundComponentState state) return;
            component.Enabled = state.Enabled;
            component.Range = state.Range;
            component.Volume = state.Volume;
        }

        private void GetCompState(EntityUid uid, AmbientSoundComponent component, ref ComponentGetState args)
        {
            args.State = new AmbientSoundComponentState
            {
                Enabled = component.Enabled,
                Range = component.Range,
                Volume = component.Volume,
            };
        }
    }
}
