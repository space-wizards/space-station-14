using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.EntityConditions.Conditions;
using Content.Shared.EntityEffects;

namespace Content.Server.EntityConditions.Conditions;

public sealed partial class BreathingEntityConditionSystem : EntityConditionSystem<RespiratorComponent, Breathing>
{
    [Dependency] private readonly RespiratorSystem _respirator = default!;
    protected override void Condition(Entity<RespiratorComponent> entity, ref EntityConditionEvent<Breathing> args)
    {
        // TODO: Conditions need handlers for if an entity without the component doesn't exist. Specifically, if something can't breathe then this should pass if "IsBreathing" is false.

        var breathingState = _respirator.IsBreathing(entity.AsNullable());
        args.Pass = args.Condition.IsBreathing == breathingState;
    }
}
