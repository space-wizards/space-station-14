using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
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
            SubscribeLocalEvent<AmbientOnPoweredComponent, PowerNetBatterySupplyEvent>(HandlePowerSupply);
        }

        private void HandlePowerSupply(EntityUid uid, AmbientOnPoweredComponent component, PowerNetBatterySupplyEvent args)
        {
            if (!EntityManager.TryGetComponent<AmbientSoundComponent>(uid, out var ambientSound)) return;
            if (ambientSound.Enabled == args.Supply) return;
            ambientSound.Enabled = args.Supply;
            ambientSound.Dirty();
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
