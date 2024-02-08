using Robust.Shared.Serialization;

namespace Content.Shared.PlantAnalyzer;

/// <summary>
///     The information about the last scanned plant/seed is stored here
/// </summary>
[Serializable, NetSerializable]
public sealed class PlantAnalyzerScannedSeedPlantInformation : BoundUserInterfaceMessage
{
    public readonly NetEntity? TargetEntity;

    public string SeedName;
    public string Endurance;
    public string SeedYield;
    public string Lifespan;
    public string Maturation;
    public string GrowthStages;
    public string SeedPotency;
    public string Repeat;
    public string SeedChem;
    public string SeedMutations;
    public string PlantSpeciation;
    public string ExudeGases;
    public Boolean IsTray;
    public Boolean ScanMode;

    public string NutrientConsumption;
    public string WaterConsumption;
    public string IdealHeat;
    public string HeatTolerance;
    public string IdealLight;
    public string LightTolerance;
    public string ToxinsTolerance;
    public string LowPresssureTolerance;
    public string HighPressureTolerance;
    public string PestTolerance;
    public string WeedTolerance;

    public PlantAnalyzerScannedSeedPlantInformation(NetEntity? targetEntit, string seedName, string seedChem, string plantHarvestType,
        string exudeGases, List<string> mutationsList, Boolean isTray, List<string> tolerancesList, List<string> generalTraitsList, Boolean scanMode)
    {
        TargetEntity = targetEntit;
        ScanMode = scanMode;

        SeedName = seedName;
        SeedChem = seedChem;
        Repeat = plantHarvestType;
        ExudeGases = exudeGases;

        IsTray = isTray;

        //SeedName = generalTraitsList[0];
        Endurance = generalTraitsList[1];
        SeedYield = generalTraitsList[2];
        Lifespan = generalTraitsList[3];
        Maturation = generalTraitsList[4];
        GrowthStages = generalTraitsList[5];
        SeedPotency = generalTraitsList[6];

        NutrientConsumption = "";
        WaterConsumption = "";
        IdealHeat = "";
        HeatTolerance = "";
        IdealLight = "";
        LightTolerance = "";
        ToxinsTolerance = "";
        LowPresssureTolerance = "";
        HighPressureTolerance = "";
        PestTolerance = "";
        WeedTolerance = "";

        SeedMutations = "";
        PlantSpeciation = "";

        if (scanMode)
        {
            NutrientConsumption = tolerancesList[0];
            WaterConsumption = tolerancesList[1];
            IdealHeat = tolerancesList[2];
            HeatTolerance = tolerancesList[3];
            IdealLight = tolerancesList[4];
            LightTolerance = tolerancesList[5];
            ToxinsTolerance = tolerancesList[6];
            LowPresssureTolerance = tolerancesList[7];
            HighPressureTolerance = tolerancesList[8];
            PestTolerance = tolerancesList[9];
            WeedTolerance = tolerancesList[10];

            SeedMutations = mutationsList[0];
            PlantSpeciation = mutationsList[1];
        }
    }
}

[Serializable, NetSerializable]
public sealed class PlantAnalyzerSetMode : BoundUserInterfaceMessage
{
    public bool AdvancedScan { get; }
    public PlantAnalyzerSetMode(bool advancedScan)
    {
        AdvancedScan = advancedScan;
    }
}
