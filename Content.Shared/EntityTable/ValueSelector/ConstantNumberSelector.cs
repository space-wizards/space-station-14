using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.EntityTable.ValueSelector;

/// <summary>
/// Gives a constant value.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ConstantNumberSelector : NumberSelector
{
    [DataField]
    public float Value = 1;

    public ConstantNumberSelector(float value)
    {
        Value = value;
    }

    public override float Get(System.Random rand, IEntityManager entMan, IPrototypeManager proto)
    {
        return Value;
    }
}
