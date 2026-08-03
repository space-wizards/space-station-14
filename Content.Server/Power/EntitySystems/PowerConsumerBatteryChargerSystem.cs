using Content.Server.Power.Components;
using Content.Shared.Power.Components;

namespace Content.Server.Power.EntitySystems;

public sealed partial class PowerConsumerBatteryChargerSystem : EntitySystem
{
    [Dependency] private BatterySystem _battery = null!;

    public override void Update(float frameTime)
    {
        var query =
            EntityQueryEnumerator<PowerConsumerComponent, PowerConsumerBatteryChargerComponent, BatteryComponent>();

        while (query.MoveNext(out var uid, out var powerConsumer, out _, out _))
        {
            var powerConsumed = powerConsumer.DrawRate * frameTime;
            _battery.ChangeCharge(uid, powerConsumed * powerConsumer.Efficiency);
        }
    }
}
