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

    private void OnPowerChanged(Entity<SpamEmitSoundRequirePowerComponent> ent, ref PowerChangedEvent args)
    {
        if (TryComp<SpamEmitSoundComponent>(ent.Owner, out var comp))
        {
            comp.Enabled = args.Powered;
        }
    }

    private void OnPowerSupply(Entity<SpamEmitSoundRequirePowerComponent> ent, ref PowerNetBatterySupplyEvent args)
    {
        if (TryComp<SpamEmitSoundComponent>(ent.Owner, out var comp))
        {
            comp.Enabled = args.Supply;
        }
    }
}
