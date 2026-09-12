using Robust.Shared.Serialization;

namespace Content.Shared.Xenoarchaeology.Artifact.Modifiers;

[Serializable, NetSerializable]
public sealed partial class WrapperModifierProvider : ModifierProviderBase
{
    [DataField]
    public ModifierProviderBase[] Nested = [];

    /// <inheritdoc />
    public WrapperModifierProvider()
    {
    }

    /// <inheritdoc />
    public WrapperModifierProvider(ModifierProviderBase[] nested)
    {
        Nested = nested;
    }

    /// <inheritdoc />
    public override float Modify(float originalValue)
    {
        var resultValue = originalValue;
        foreach (var modifier in Nested)
        {
            resultValue = modifier.Modify(resultValue);
        }

        return resultValue;
    }
}
