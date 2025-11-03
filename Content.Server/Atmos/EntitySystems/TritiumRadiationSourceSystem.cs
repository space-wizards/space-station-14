using Content.Server.Atmos.Components;
using Content.Shared.Radiation.Components;

namespace Content.Server.Atmos.EntitySystems;

public sealed class TritiumRadiationSourceSystem : EntitySystem
{
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TritiumRadiationSourceComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            component.Lifetime -= frameTime;
            if (component.Lifetime <= 0)
            {
                RemComp<RadiationSourceComponent>(uid);
                RemComp<TritiumRadiationSourceComponent>(uid);
            }
        }
    }
}
