using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Sound.Components;

namespace Content.Server.Sound;

public sealed partial class EmitSoundIntervalRequirePowerSystem : EntitySystem
{
    [Dependency] private readonly EmitSoundSystem _sound = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmitSoundIntervalRequirePowerComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<EmitSoundIntervalRequirePowerComponent, PowerNetBatterySupplyEvent>(OnPowerSupply);
    }

    private void OnPowerChanged(EntityUid uid, EmitSoundIntervalRequirePowerComponent component, ref PowerChangedEvent args)
    {
        if (TryComp<EmitSoundIntervalComponent>(uid, out var comp))
        {
            comp.Enabled = args.Powered;
        }
    }

    private void OnPowerSupply(EntityUid uid, EmitSoundIntervalRequirePowerComponent component, ref PowerNetBatterySupplyEvent args)
    {
        if (TryComp<EmitSoundIntervalComponent>(uid, out var comp))
        {
            comp.Enabled = args.Supply;
        }
    }
}
