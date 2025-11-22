using Content.Shared.Random.Helpers;
using Content.Shared.Trigger.Components.Conditions;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Trigger.Systems.Conditions;

public sealed class RandomChanceTriggerConditionSystem : TriggerConditionSystem<RandomChanceTriggerConditionComponent>
{
    [Dependency] private readonly IGameTiming _timing = default!;

    protected override void CheckCondition(Entity<RandomChanceTriggerConditionComponent> ent, ref AttemptTriggerEvent args)
    {
        // TODO: Replace with RandomPredicted once the engine PR is merged
        var hash = new List<int>
        {
            (int)_timing.CurTick.Value,
            GetNetEntity(ent).Id,
            args.User == null ? 0 : GetNetEntity(args.User.Value).Id,
        };
        var seed = SharedRandomExtensions.HashCodeCombine(hash);
        var rand = new System.Random(seed);

        var cancel = !rand.Prob(ent.Comp.SuccessChance); // When unsuccessful, cancel = true
        ModifyEvent(ent, cancel, ref args);
    }
}
