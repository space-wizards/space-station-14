using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared.Xenoarchaeology.Artifact.Modifiers;
[Serializable, NetSerializable]
public sealed partial class BudgetDependantAddModifierProvider : ModifierProviderBase, IBudgetPlacementAwareModifier
{
    [DataField]
    public Vector2 Range = new(1, 1);

    [DataField]
    public float? RangeCenter;

    [DataField]
    public float? PlacementInBudget { get; set; }

    /// <inheritdoc />
    public override float Modify(float originalValue)
    {
        // modifier provider in budget dependent, if no placement was provided - do nothing
        if (!PlacementInBudget.HasValue)
            return originalValue;

        var min = Range.X;
        var max = Range.Y;
        // if range center provided - use it, otherwise calculate
        float rangeCenter;
        if (RangeCenter.HasValue)
            rangeCenter = RangeCenter.Value;
        else
            rangeCenter = (float)(max + min) / 2;

        // direct center of placement range corresponds to center of provided value change range
        if (PlacementInBudget == 0)
            return rangeCenter + originalValue;

        float distanceFromCenter;
        if (PlacementInBudget.Value > 0)
            distanceFromCenter = max - rangeCenter;
        else
            distanceFromCenter = rangeCenter - min;

        // distanceFromCenter = Math.Abs(distanceFromCenter);

        var valueChange = rangeCenter + distanceFromCenter * PlacementInBudget.Value;
        return valueChange + originalValue;
    }
}
