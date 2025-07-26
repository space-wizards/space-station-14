using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Reagent;

/// <summary>
/// This is a reagentData class that works just like DNA,
/// which can be added to a reagent.
/// Look at the function GetEntityBloodData() in BloodstreamSystem.cs
/// for reference.
/// ReagentData can be dynamically resolved depending
/// on the presence and configuration of other components on an
/// entity handling a reagent e.g. blood in a humanoid
/// or reagents in a solution in a chem dispenser.
/// The resolved list can then be cached in a ReagentId
/// object with EnsureReagentData().
/// </summary>
[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public sealed partial class ReagentColorData : ReagentData
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

        return ((ReagentColorData)other).SubstanceColor == SubstanceColor;
    }

    public override int GetHashCode()
    {
        return SubstanceColor.GetHashCode();
    }
}
