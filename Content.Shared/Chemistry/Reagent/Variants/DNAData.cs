using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Reagent.Variants;

[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public sealed partial class DnaData : ReagentVariant
{
    [DataField]
    public string DNA = String.Empty;

    public override ReagentVariant Clone() => this;

    public override bool Equals(ReagentVariant? other)
    {
        if (other == null)
        {
            return false;
        }

        return ((DnaData) other).DNA == DNA;
    }
    public override int GetHashCode()
    {
        return DNA.GetHashCode();
    }
}
