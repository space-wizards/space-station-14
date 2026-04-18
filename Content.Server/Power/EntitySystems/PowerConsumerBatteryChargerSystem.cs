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

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<PowerConsumerBatteryChargerComponent, PowerConsumerComponent, BatteryComponent>();
        while (query.MoveNext(out var entityUid, out var powerConsumerBatteryCharger, out var powerConsumer, out var battery))
        {
            var energyConsumed = powerConsumer.ReceivedPower * frameTime;

            if (energyConsumed == 0)
                continue;

            _battery.ChangeCharge((entityUid, battery), energyConsumed * powerConsumerBatteryCharger.Efficiency);
        }
    }
}
