using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Sound.Components;

namespace Content.Server.Sound;

public sealed partial class SpamEmitSoundRequirePowerSystem : EntitySystem
{
    [Dependency] private readonly EmitSoundSystem _emitSound = default!;

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
            _emitSound.SetEnabled((entity, comp), args.Powered);
        }
    }

    private void OnPowerSupply(Entity<SpamEmitSoundRequirePowerComponent> entity, ref PowerNetBatterySupplyEvent args)
    {
        if (TryComp<SpamEmitSoundComponent>(entity.Owner, out var comp))
        {
            _emitSound.SetEnabled((entity, comp), args.Supply);
        }
    }
}
