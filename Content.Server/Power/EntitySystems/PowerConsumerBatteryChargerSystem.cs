using Content.Server.Power.Components;
using Content.Shared.Power.Components;

namespace Content.Server.Power.EntitySystems;

public sealed class PowerConsumerBatteryChargerSystem : EntitySystem
{
    [Dependency] private readonly BatterySystem _battery = null!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PowerConsumerEfficiencyVoltageTogglerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PowerConsumerEfficiencyVoltageTogglerComponent, VoltageChangeEvent>(OnVoltageChanged);

        SubscribeLocalEvent<PowerConsumerBatteryChargerComponent, PowerConsumedEvent>(OnPowerConsumed);
    }

    private void OnMapInit(
        Entity<PowerConsumerEfficiencyVoltageTogglerComponent> entity,
        ref MapInitEvent args)
    {
        if (!TryComp<PowerConsumerComponent>(entity, out var powerConsumer))
            return;

        powerConsumer.Efficiency = entity.Comp.EfficiencyPerVoltage[powerConsumer.Voltage];
    }

    private void OnVoltageChanged(
        Entity<PowerConsumerEfficiencyVoltageTogglerComponent> entity,
        ref VoltageChangeEvent args)
    {
        if (!TryComp<PowerConsumerComponent>(entity, out var powerConsumer))
            return;

        powerConsumer.Efficiency = entity.Comp.EfficiencyPerVoltage[args.NewVoltage.Voltage];
    }

    private void OnPowerConsumed(Entity<PowerConsumerBatteryChargerComponent> entity, ref PowerConsumedEvent args)
    {
        if (!TryComp<BatteryComponent>(entity, out var battery))
            return;

        _battery.ChangeCharge((entity, battery), args.EffectivePower);
    }
}
