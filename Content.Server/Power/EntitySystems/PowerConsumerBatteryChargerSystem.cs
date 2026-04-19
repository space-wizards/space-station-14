using Content.Server.Power.Components;
using Content.Shared.Power.Components;

namespace Content.Server.Power.EntitySystems;

public sealed class PowerConsumerBatteryChargerSystem : EntitySystem
{
    [Dependency] private readonly BatterySystem _battery = null!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PowerConsumerBatteryChargerEfficiencyVoltageTogglerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PowerConsumerBatteryChargerEfficiencyVoltageTogglerComponent, VoltageChangeEvent>(OnVoltageChanged);

        SubscribeLocalEvent<PowerConsumerBatteryChargerComponent, PowerConsumedEvent>(OnPowerConsumed);
    }

    private void OnMapInit(
        Entity<PowerConsumerBatteryChargerEfficiencyVoltageTogglerComponent> entity,
        ref MapInitEvent args)
    {
        if (!TryComp<PowerConsumerBatteryChargerComponent>(entity, out var powerConsumerBatteryCharger))
            return;

        if (!TryComp<PowerConsumerComponent>(entity, out var powerConsumer))
            return;

        powerConsumerBatteryCharger.Efficiency = entity.Comp.EfficiencyPerVoltage[powerConsumer.Voltage];
    }

    private void OnVoltageChanged(
        Entity<PowerConsumerBatteryChargerEfficiencyVoltageTogglerComponent> entity,
        ref VoltageChangeEvent args)
    {
        if (!TryComp<PowerConsumerBatteryChargerComponent>(entity, out var powerConsumerBatteryCharger))
            return;

        powerConsumerBatteryCharger.Efficiency = entity.Comp.EfficiencyPerVoltage[args.NewVoltage.Voltage];
    }

    private void OnPowerConsumed(Entity<PowerConsumerBatteryChargerComponent> entity, ref PowerConsumedEvent args)
    {
        if (!TryComp<BatteryComponent>(entity, out var battery))
            return;

        _battery.ChangeCharge((entity, battery), args.PowerConsumed * entity.Comp.Efficiency);
    }
}
