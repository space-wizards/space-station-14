using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Audio;

namespace Content.Server.Audio
{
    public sealed class AmbientSoundSystem : SharedAmbientSoundSystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<AmbientOnPoweredComponent, PowerChangedEvent>(HandlePowerChange);
            SubscribeLocalEvent<AmbientOnPoweredComponent, PowerNetBatterySupplyEvent>(HandlePowerSupply);
        }

        private void HandlePowerSupply(EntityUid uid, AmbientOnPoweredComponent component, ref PowerNetBatterySupplyEvent args)
        {
            if (!EntityManager.TryGetComponent<AmbientSoundComponent>(uid, out var ambientSound)) return;
            if (ambientSound.Enabled == args.Supply) return;
            ambientSound.Enabled = args.Supply;
            Dirty(ambientSound);
        }

        private void HandlePowerChange(EntityUid uid, AmbientOnPoweredComponent component, ref PowerChangedEvent args)
        {
            if (!EntityManager.TryGetComponent<AmbientSoundComponent>(uid, out var ambientSound)) return;
            if (ambientSound.Enabled == args.Powered) return;
            ambientSound.Enabled = args.Powered;
            Dirty(ambientSound);
        }
    }
}
