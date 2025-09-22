using Content.Shared.EntityEffects;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Shared.EntityConditions.Conditions;

/// <summary>
/// Returns true if current mob state matches given mob state.
/// </summary>
public sealed partial class MobStateEntityConditionSystem : EntityConditionSystem<MobStateComponent, IsMobState>
{
    protected override void Condition(Entity<MobStateComponent> entity, ref EntityConditionEvent<IsMobState> args)
    {
        if (entity.Comp.CurrentState == args.Condition.Mobstate)
            args.Result = true;
    }
}

[DataDefinition]
public sealed partial class IsMobState : EntityConditionBase<IsMobState>
{
    [DataField]
    public MobState Mobstate = MobState.Alive;
}
