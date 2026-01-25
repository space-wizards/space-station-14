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
        SubscribeLocalEvent<SunShadowCycleComponent, LightCycleOffsetEvent>(OnCycleOffset);
    }

    private void OnCycleOffset(Entity<SunShadowCycleComponent> ent, ref LightCycleOffsetEvent args)
    {
        // Okay so we synchronise with LightCycleComponent.
        // However, the offset is only set on MapInit and we have no guarantee which one is ran first so we make sure.
        ent.Comp.Offset = args.Offset;
        Dirty(ent);
    }

    private void OnCycleMapInit(Entity<SunShadowCycleComponent> ent, ref MapInitEvent args)
    {
        if (TryComp(ent.Owner, out LightCycleComponent? lightCycle))
        {
            ent.Comp.Duration = lightCycle.Duration;
            ent.Comp.Offset = lightCycle.Offset;
        }
        else
        {
            ent.Comp.Offset = _random.Next(ent.Comp.Duration);
        }

        Dirty(ent);
    }
}
