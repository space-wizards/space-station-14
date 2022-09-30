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

        foreach (var comp in EntityQuery<RevenantOverloadedLightsComponent>())
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
