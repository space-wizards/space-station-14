using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Reagent;


/// <summary>
///     Class used only to cache frequently accessed prototype data in solutions.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ReagentProprieties
{
    public float SpecificHeat;
    
    public float BoilingPoint;
    public float MeltingPoint;

    public float BoilingLatentHeat;
    public float MeltingLatentHeat;

    public ReagentProprieties(
        float specificHeat = 1, 
        float boilingPoint = float.PositiveInfinity, 
        float meltingPoint = float.NegativeInfinity, 
        float boilingLatentHeat = float.PositiveInfinity,
        float meltingLatentHeat = float.NegativeInfinity)
    {
        SpecificHeat = specificHeat;
        BoilingPoint = boilingPoint;
        MeltingPoint = meltingPoint;
        BoilingLatentHeat = boilingLatentHeat;
        MeltingLatentHeat = meltingLatentHeat;
    }
}