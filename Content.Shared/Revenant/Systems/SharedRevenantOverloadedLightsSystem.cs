using Content.Shared.Revenant.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Revenant.Systems;

public abstract class SharedRevenantOverloadedLightsSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<RevenantOverloadedLightsComponent>();

        while (enumerator.MoveNext(out var uid, out var comp))
        {
            // Don't try to zap if we don't have a target yet
            if (Timing.CurTime < comp.NextZapTime || comp.Target == null)
                continue;

            OnZap((uid, comp));
            RemCompDeferred(uid, comp);
        }
    }

    /// <summary>
    ///     Sets a target to zap for a light.
    /// </summary>
    /// <param name="ent">The light entity.</param>
    /// <param name="target">The target to set.</param>
    public void SetZapTarget(Entity<RevenantOverloadedLightsComponent?> ent, EntityUid target)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.Target = target;
        ent.Comp.NextZapTime = Timing.CurTime + ent.Comp.ZapDelay; // we want to set the time as soon as we get the target
    }

    protected abstract void OnZap(Entity<RevenantOverloadedLightsComponent> component);
}
