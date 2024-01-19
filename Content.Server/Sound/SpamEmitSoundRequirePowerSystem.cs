using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Sound.Components;

namespace Content.Server.Sound;

public sealed partial class SpamEmitSoundRequirePowerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpamEmitSoundRequirePowerComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<SpamEmitSoundRequirePowerComponent, PowerNetBatterySupplyEvent>(OnPowerSupply);
    }

    private void OnPowerChanged(EntityUid uid, SpamEmitSoundRequirePowerComponent component, ref PowerChangedEvent args)
    {
        if (TryComp<SpamEmitSoundComponent>(uid, out var comp))
        {
            comp.Enabled = args.Powered;
        }
    }

    private void OnPowerSupply(EntityUid uid, SpamEmitSoundRequirePowerComponent component, ref PowerNetBatterySupplyEvent args)
    {
        if (TryComp<SpamEmitSoundComponent>(uid, out var comp))
        {
            comp.Enabled = args.Supply;
        }
    }
}
