using Robust.Shared.Serialization;

namespace Content.Shared.PlantAnalyzer;

/// <summary>
///     The information about the last scanned plant is stored here
/// </summary>
[Serializable, NetSerializable]
public sealed class PlantAnalyzerScannedSeedPlantInformation : BoundUserInterfaceMessage
{
    public readonly NetEntity? TargetEntity;
    public float SeedEndurance;

    public string SeedYield = "";
    public string SeedPotency = "";
    public string SeedChem = "";
    public string SeedHarvestType = "";

    public float SeedMinTemp;
    public float SeedMaxTemp;

    public string SeedMut = "";

    public string SeedName = "";
    public float SeedHealth;
    public string SeedProblems = "";

    public bool IsTray;
    public PlantAnalyzerScannedSeedPlantInformation(NetEntity? targetEntity)
    {
        TargetEntity = targetEntity;

    }
}
