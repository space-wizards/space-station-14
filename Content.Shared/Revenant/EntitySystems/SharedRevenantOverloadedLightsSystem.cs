using Content.Shared.Revenant.Components;

namespace Content.Shared.Revenant.EntitySystems;

/// <summary>
/// This handles...
/// </summary>
public abstract class SharedRevenantOverloadedLightsSystem : EntitySystem
{
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<RevenantOverloadedLightsComponent>();

        while (enumerator.MoveNext(out var comp))
        {
            comp.Accumulator += frameTime;

            if (comp.Accumulator < comp.ZapDelay)
                continue;

            OnZap(comp);
            RemComp(comp.Owner, comp);
        }
    }

    protected abstract void OnZap(RevenantOverloadedLightsComponent component);
}
