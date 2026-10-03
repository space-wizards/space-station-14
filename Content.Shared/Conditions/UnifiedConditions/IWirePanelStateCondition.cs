using Content.Shared.Wires;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface IWirePanelStateCondition : IConditionByEvent<IWirePanelStateCondition>
{
    bool Open { get; }
}

/// <summary>
/// Returns true if this solution entity has an amount of reagent in it within a specified minimum and maximum.
/// </summary>
public sealed partial class WirePanelStateConditionSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void Condition(Entity<MetaDataComponent> entity,
        ref ConditionEvaluationEvent<IWirePanelStateCondition> args)
    {
        args.Handled = true;
        //if it doesn't have a wire panel, then just let it work.
        if (!TryComp<WiresPanelComponent>(entity.Owner, out var wires))
        {
            args.Value = 1;
            return;
        }

        args.Value = wires.Open == args.Condition.Open ? 1 : 0;
    }
}
