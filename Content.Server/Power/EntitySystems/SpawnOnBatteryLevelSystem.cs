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

        if (battery.LastCharge >= entity.Comp.Charge)
        {
            Spawn(entity.Comp.Prototype, xform.Coordinates);

            _battery.ChangeCharge((entity, battery), -entity.Comp.Charge);
        }

    }
}
