using Content.Server.Power.Components;
using Content.Shared.Power.Components;

namespace Content.Server.Power.EntitySystems;

public sealed class PowerConsumerBatteryChargerSystem : EntitySystem
{
    [Dependency] private readonly BatterySystem _battery = null!;

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
