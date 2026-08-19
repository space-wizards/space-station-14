using Content.Server.Power.Components;
using Content.Shared.Power.Components;

namespace Content.Server.Power.EntitySystems;

public sealed partial class PowerConsumerBatteryChargerSystem : EntitySystem
{
    [Dependency] private BatterySystem _battery = null!;

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<PowerConsumerComponent, PowerConsumerBatteryChargerComponent, BatteryComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var powerConsumer, out var batteryCharger, out _, out var transformComp))
        {
            if (!transformComp.Anchored)
                continue;

            var powerConsumed = powerConsumer.ReceivedPower * frameTime;
            _battery.ChangeCharge(uid, powerConsumed * batteryCharger.Efficiency);
        }
    }
}
