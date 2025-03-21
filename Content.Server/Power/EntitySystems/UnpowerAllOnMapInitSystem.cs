using Content.Server.Power.Components;
using Robust.Shared.Random;

namespace Content.Server.Power.EntitySystems;

/// <summary>
/// Handles unpowering entities on map init>
/// </summary>
public sealed partial class UnpowerAllOnMapInitSystem : EntitySystem
{
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UnpowerOnMapInitComponent, MapInitEvent>(OnMapInit, after: [typeof(BatterySystem)]);
    }

    private void OnMapInit(Entity<UnpowerOnMapInitComponent> entity, ref MapInitEvent args)
    {
        if (TryComp<BatteryComponent>(entity, out var battery) && _random.Prob(0.9f)) // this is a piece of code for one day, i am hardcoding this
        {
            _battery.SetCharge(entity, 0f);
        }

        // Our work here is done
        RemCompDeferred(entity, entity.Comp);
    }
}
