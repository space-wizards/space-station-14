using Robust.Shared.Serialization;

namespace Content.Shared.PlantAnalyzer;

/// <summary>
///     The information about the last scanned plant/seed is stored here
/// </summary>
[Serializable, NetSerializable]
public sealed class PlantAnalyzerScannedSeedPlantInformation : BoundUserInterfaceMessage
{
    public readonly NetEntity? TargetEntity;

    public string SeedName = "";
    public string SeedYield = "";
    public string SeedPotency = "";
    public string Repeat = "";
    public string SeedChem = "";
    public string SeedMutations = "";
    public string ExudeGases = "";
    public Boolean IsTray;

    public PlantAnalyzerScannedSeedPlantInformation(NetEntity? targetEntit, string seedName, string seedYield, string seedPotency,
        string seedChem, string plantHarvestType, string exudeGases, string seedMutations, Boolean isTray)
    {
        TargetEntity = targetEntit;

        SeedName = seedName;
        SeedYield = seedYield;
        SeedPotency = seedPotency;
        SeedChem = seedChem;
        Repeat = plantHarvestType;

        ExudeGases = exudeGases;
        SeedMutations = seedMutations;
        IsTray = isTray;
    }
}
