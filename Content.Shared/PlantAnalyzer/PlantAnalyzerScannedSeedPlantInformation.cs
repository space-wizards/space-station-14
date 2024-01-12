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

    public string NutrientConsumption = "";
    public string WaterConsumption = "";
    public string IdealHeat = "";
    public string HeatTolerance = "";
    public string IdealLight = "";
    public string LightTolerance = "";
    public string ToxinsTolerance = "";
    public string LowPresssureTolerance = "";
    public string HighPressureTolerance = "";
    public string PestTolerance = "";
    public string WeedTolerance = "";

    public string Lifespan = "";
    public string Maturation = "";
    public string GrowthStages = "";
    public string PlantSpeciation = "";
    public string Tolerances = "";

    public PlantAnalyzerScannedSeedPlantInformation(NetEntity? targetEntit, string seedName, string seedYield, string seedPotency,
        string seedChem, string plantHarvestType, string exudeGases, string seedMutations, Boolean isTray, string plantSpeciation, string tolerances)
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


        PlantSpeciation = plantSpeciation;

        Tolerances = tolerances;

        String[] arr = tolerances.Split(";");

        NutrientConsumption = arr[0];
        WaterConsumption = arr[1];
        IdealHeat = arr[2];
        HeatTolerance = arr[3];
        IdealLight = arr[4];
        LightTolerance = arr[5];
        ToxinsTolerance = arr[6];
        LowPresssureTolerance = arr[7];
        HighPressureTolerance = arr[8];
        PestTolerance = arr[9];
        WeedTolerance = arr[10];

        Lifespan = arr[11];
        Maturation = arr[12];
        GrowthStages = arr[13];



    }
}
