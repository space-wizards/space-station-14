using Content.Shared.Mobs.Systems;
using Content.Shared.StepTrigger.Components;

namespace Content.Shared.StepTrigger.Systems;

/// <inheritdoc cref="StepTriggerOnAliveComponent"/>
public sealed class StepTriggerOnAliveSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<StepTriggerOnAliveComponent, StepTriggerAttemptEvent>(OnStepTriggerOnAliveAttempt);
    }

    private void OnStepTriggerOnAliveAttempt(Entity<StepTriggerOnAliveComponent> ent, ref StepTriggerAttemptEvent args)
    {
        var alive = _mobState.IsAlive(ent);

        args.Continue = alive;
        args.Cancelled = !alive;
    }
}
