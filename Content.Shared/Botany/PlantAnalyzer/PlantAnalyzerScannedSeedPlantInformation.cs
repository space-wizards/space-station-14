using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Botany.PlantAnalyzer;

/// <summary>
///     The information about the last scanned plant is stored here
/// </summary>


[Serializable, NetSerializable]
public sealed class PlantAnalyzerScannedSeedPlantInformation : BoundUserInterfaceState
{
    public readonly EntityUid? TargetEntity;
    public readonly float seedEndurance;

    public readonly string seedYield = "";
    public readonly string seedPotency= "";
    public readonly string seedChem= "";
    public readonly string seedHarvestType= "";

    public readonly float seedMinTemp;
    public readonly float seedMaxTemp;

    public readonly string seedMut = "";

    public readonly  string seedName = " ";
    public readonly float seedHealth;
    public readonly string seedProblems;

    public readonly bool isTray;

    public PlantAnalyzerScannedSeedPlantInformation(EntityUid? targetEntity,float SeedEndurance, string SeedYield, string SeedPotency,
        string seedharvestType, string SeedChem,float SeedMinTemp, float SeedMaxTemp, string PlantMut, string SeedName,
        float SeedHealth, string SeedProblems, bool IsTray )
    {
        TargetEntity = targetEntity;
        seedEndurance = SeedEndurance;

        seedYield = SeedYield;
        seedPotency = SeedPotency;
        seedChem = SeedChem;
        seedHarvestType = seedharvestType;
        seedMinTemp = SeedMinTemp;
        seedMaxTemp = SeedMaxTemp;
        seedMut = PlantMut;

        seedName = SeedName;
        seedHealth = SeedHealth;
        seedProblems = SeedProblems;

        isTray = IsTray;
    }
}
