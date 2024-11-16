using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Reagent;

[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public sealed partial class DnaData : ReagentData
{
    [DataField]
    public string DNA = String.Empty;

    public override ReagentData Clone() => this;

    public override bool Equals(ReagentData? other)
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
