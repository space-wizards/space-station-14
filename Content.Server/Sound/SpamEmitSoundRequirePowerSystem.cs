using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Sound;
using Content.Shared.Sound.Components;

namespace Content.Server.Sound;

public sealed partial class SpamEmitSoundRequirePowerSystem : SharedSpamEmitSoundRequirePowerSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpamEmitSoundRequirePowerComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<SpamEmitSoundRequirePowerComponent, PowerNetBatterySupplyEvent>(OnPowerSupply);
    }

    private void OnPowerChanged(Entity<SpamEmitSoundRequirePowerComponent> entity, ref PowerChangedEvent args)
    {
        if (TryComp<SpamEmitSoundComponent>(entity.Owner, out var comp))
        {
            EmitSound.SetEnabled((entity, comp), args.Powered);
        }
    }

    private void OnPowerSupply(Entity<SpamEmitSoundRequirePowerComponent> entity, ref PowerNetBatterySupplyEvent args)
    {
        if (TryComp<SpamEmitSoundComponent>(entity.Owner, out var comp))
        {
            EmitSound.SetEnabled((entity, comp), args.Supply);
        }
    }
}
