using Content.Shared.Station.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Generic;

/// <summary>
/// Returns true if entity is on a station grid
/// </summary>
public sealed class OnStationGridConditionSystem : EntityConditionSystem<TransformComponent, OnStationGridCondition>
{
    protected override void Condition(Entity<TransformComponent> entity, ref EntityConditionEvent<OnStationGridCondition> args)
    {
        args.Result = HasComp<StationMemberComponent>(entity.Comp.GridUid);
    }
}


/// <inheritdoc cref="EntityCondition"/>
public sealed partial class OnStationGridCondition : EntityConditionBase<OnStationGridCondition>
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype) => String.Empty;
}
