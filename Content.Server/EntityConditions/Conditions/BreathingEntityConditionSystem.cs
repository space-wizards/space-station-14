using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.EntityConditions.Conditions;
using Content.Shared.EntityEffects;

namespace Content.Server.EntityConditions.Conditions;

public sealed partial class BreathingEntityConditionSystem : EntityConditionSystem<RespiratorComponent, Shared.EntityConditions.Conditions.Body.IsBreathing>
{
    [Dependency] private readonly RespiratorSystem _respirator = default!;
    protected override void Condition(Entity<RespiratorComponent> entity, ref EntityConditionEvent<Shared.EntityConditions.Conditions.Body.IsBreathing> args)
    {
        var breathingState = _respirator.IsBreathing(entity.AsNullable());
        args.Result = breathingState;
    }
}
