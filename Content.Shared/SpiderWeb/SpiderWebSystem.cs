using Content.Shared.StepTrigger.Systems;

namespace Content.Shared.SpiderWeb;

/// <summary>
///     Cancels step triggers between <see cref="SpiderWebObjectComponent"/> and <see cref="IgnoreSpiderWebComponent"/>.
///     Collide triggers must be ignored elsewhere.
/// </summary>
public sealed class SpiderWebSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpiderWebObjectComponent, StepTriggerAttemptEvent>(HandleStepTrigger);
    }

    private void HandleStepTrigger(Entity<SpiderWebObjectComponent> _, ref StepTriggerAttemptEvent args)
    {
        if (HasComp<IgnoreSpiderWebComponent>(args.Tripper))
            args.Cancelled = true;
    }
}
