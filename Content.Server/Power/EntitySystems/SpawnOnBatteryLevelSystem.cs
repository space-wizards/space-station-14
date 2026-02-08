using Content.Server.Power.Components;
using Content.Shared.Power;
using Content.Shared.Power.Components;

namespace Content.Server.Power.EntitySystems;

public sealed class SpawnOnBatteryLevelSystem : EntitySystem
{
    [Dependency] private readonly BatterySystem _battery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpawnOnBatteryLevelComponent, ChargeChangedEvent>(OnBatteryChargeChange);
    }

    private void OnBatteryChargeChange(Entity<SpawnOnBatteryLevelComponent> entity, ref ChargeChangedEvent args)
    {
        if (!TryComp<BatteryComponent>(entity, out var battery))
            return;

        if (!TryComp(entity, out TransformComponent? xform))
            return;

        var spawnFlag = false;
        var powerToConsume = 0f;

        if (entity.Comp.Level != null)
        {
            spawnFlag = battery.LastCharge / battery.MaxCharge >= entity.Comp.Level;
            powerToConsume = (entity.Comp.Level * battery.MaxCharge).Value;
        }
        else if (entity.Comp.Charge != null)
        {
            spawnFlag = battery.LastCharge >= entity.Comp.Charge;
            powerToConsume = entity.Comp.Charge.Value;
        }

        if (spawnFlag)
        {
            Spawn(entity.Comp.Prototype, xform.Coordinates);

            _battery.ChangeCharge((entity, battery), -powerToConsume);
        }

    }
}
