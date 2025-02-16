using Content.Shared.Light.Components;
using Robust.Shared.Random;

namespace Content.Shared.Light.EntitySystems;

public abstract class SharedSunShadowSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SunShadowCycleComponent, MapInitEvent>(OnCycleMapInit);
    }

    private void OnCycleMapInit(Entity<SunShadowCycleComponent> ent, ref MapInitEvent args)
    {
        if (TryComp(ent.Owner, out LightCycleComponent? lightCycle))
        {
            ent.Comp.Offset = lightCycle.Offset;
            ent.Comp.Duration = lightCycle.Duration;
        }
        else
        {
            ent.Comp.Offset = _random.Next(ent.Comp.Duration);
        }

        Dirty(ent);
    }
}
