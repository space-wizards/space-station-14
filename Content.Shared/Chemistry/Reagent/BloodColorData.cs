using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Reagent;

[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public sealed partial class BloodColorData : ReagentData
{
    [DataField]
    public Color SubstanceColor = Color.White;

    public override ReagentData Clone() => this;

    public override bool Equals(ReagentData? other)
    {
        if (other == null)
        {
            return false;
        }

        return ((BloodColorData) other).SubstanceColor == SubstanceColor;
    }

    public override int GetHashCode()
    {
        return SubstanceColor.GetHashCode();
    }
}
