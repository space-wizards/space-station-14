using Content.Shared.Trigger;
using Content.Shared.Trigger.Components.Conditions;
using Robust.Shared.Random;

namespace Content.Server.Trigger.Systems;

/// <summary>
/// System for randomly failing a trigger.
/// TODO move to TriggerSystem.Condition.cs when randomness is predicted.
/// </summary>
public sealed class RandomChanceTriggerConditionSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomChanceTriggerConditionComponent, AttemptTriggerEvent>(OnRandomChanceTriggerAttempt);
    }

    private void OnRandomChanceTriggerAttempt(Entity<RandomChanceTriggerConditionComponent> ent, ref AttemptTriggerEvent args)
    {
        if (args.Key == null || ent.Comp.Keys.Contains(args.Key))
            args.Cancelled |= _random.Prob(ent.Comp.FailureChance);
    }
}
