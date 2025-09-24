using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.EntityConditions.Conditions.Body;
using Content.Shared.EntityEffects;

namespace Content.Server.EntityConditions.Conditions;

public sealed partial class IsBreathingEntityConditionSystem : EntityConditionSystem<RespiratorComponent, IsBreathing>
{
    [Dependency] private readonly RespiratorSystem _respirator = default!;
    protected override void Condition(Entity<RespiratorComponent> entity, ref EntityConditionEvent<IsBreathing> args)
    {
        var breathingState = _respirator.IsBreathing(entity.AsNullable());
        args.Result = breathingState;
    }
}
