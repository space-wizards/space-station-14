using Content.Server.Power.Components;
using Content.Shared.Audio;
using Robust.Shared.GameObjects;

namespace Content.Server.Audio
{
    public sealed class AmbientSoundSystem : SharedAmbientSoundSystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<AmbientOnPoweredComponent, PowerChangedEvent>(HandlePowerChange);
        }

        private void HandlePowerChange(EntityUid uid, AmbientOnPoweredComponent component, PowerChangedEvent args)
        {
            if (!EntityManager.TryGetComponent<AmbientSoundComponent>(uid, out var ambientSound)) return;
            if (ambientSound.Enabled == args.Powered) return;
            ambientSound.Enabled = args.Powered;
            ambientSound.Dirty();
        }
    }
}
