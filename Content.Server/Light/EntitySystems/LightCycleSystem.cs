using Content.Shared;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Robust.Shared.Random;

namespace Content.Server.Light.EntitySystems;

/// <inheritdoc/>
public sealed partial class LightCycleSystem : SharedLightCycleSystem
{
    [Dependency] private IRobustRandom _random = default!;

    protected override void OnCycleMapInit(Entity<LightCycleComponent> ent, ref MapInitEvent args)
    {
        base.OnCycleMapInit(ent, ref args);

        if (ent.Comp.InitialOffset)
        {
            SetOffset(ent, _random.Next(ent.Comp.Duration));
        }
    }
}
