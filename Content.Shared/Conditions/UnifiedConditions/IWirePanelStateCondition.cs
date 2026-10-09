using Content.Shared.Wires;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface IWirePanelStateCondition : ICondition
{
    bool Open { get; }
}

/// <summary>
/// Returns true if this solution entity has an amount of reagent in it within a specified minimum and maximum.
/// </summary>
public sealed partial class WirePanelStateConditionSystem : ConditionEvaluatorSystem<IWirePanelStateCondition>
{
    public override float Evaluate(IWirePanelStateCondition condition, EntityUid entityUid, EntityUid? sourceEntity = null)
    {
        //if it doesn't have a wire panel, then just let it work.
        if (!TryComp<WiresPanelComponent>(entityUid, out var wires))
        {
            return 1;
        }

        return wires.Open == condition.Open ? 1 : 0;
    }
}
