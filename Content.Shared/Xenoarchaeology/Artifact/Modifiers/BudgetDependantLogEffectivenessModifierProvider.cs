using Robust.Shared.Serialization;

namespace Content.Shared.Xenoarchaeology.Artifact.Modifiers;

[Serializable, NetSerializable]
public sealed partial class BudgetDependantLogEffectivenessModifierProvider : ModifierProviderBase, IBudgetPlacementAwareModifier
{
    [DataField]
    public int LogBase = 2;

    [DataField]
    public float IncrementToOffsetZero = 2;

    [DataField]
    public float? PlacementInBudget { get; set; }

    /// <inheritdoc />
    public override float Modify(float originalValue)
    {
        if (!PlacementInBudget.HasValue)
            return originalValue;

        PlacementInBudget += IncrementToOffsetZero;
        return MathF.Log(PlacementInBudget.Value, LogBase) * originalValue;
    }
}
