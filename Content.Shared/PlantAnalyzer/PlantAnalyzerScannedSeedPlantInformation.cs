using Robust.Shared.Serialization;

namespace Content.Shared.PlantAnalyzer;

/// <summary>
///     The information about the last scanned plant/seed is stored here
/// </summary>
[Serializable, NetSerializable]
public sealed class PlantAnalyzerScannedSeedPlantInformation : BoundUserInterfaceMessage
{
    public readonly NetEntity? TargetEntity;

    public Boolean ScanMode;

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
    public string Endurance = "";

    public PlantAnalyzerScannedSeedPlantInformation(NetEntity? targetEntit, string seedName, string seedChem, string plantHarvestType,
        string exudeGases, string potency, string yield, string seedMutations, Boolean isTray, string plantSpeciation, string tolerances, string generalTraits, Boolean scanMode)
    {
        TargetEntity = targetEntit;
        ScanMode = scanMode;

        SeedName = seedName;
        SeedChem = seedChem;
        Repeat = plantHarvestType;
        ExudeGases = exudeGases;
        SeedYield = yield;
        SeedPotency = potency;

        IsTray = isTray;

        SeedMutations = seedMutations;
        PlantSpeciation = plantSpeciation;

        if (scanMode || SeedName.Contains("NanoTrasen")) //advanced scan mode OR its a roundstart prototype seed
        {
            String[] arrTol = tolerances.Split(";");
            NutrientConsumption = arrTol[0];
            WaterConsumption = arrTol[1];
            IdealHeat = arrTol[2];
            HeatTolerance = arrTol[3];
            IdealLight = arrTol[4];
            LightTolerance = arrTol[5];
            ToxinsTolerance = arrTol[6];
            LowPresssureTolerance = arrTol[7];
            HighPressureTolerance = arrTol[8];
            PestTolerance = arrTol[9];
            WeedTolerance = arrTol[10];

            String[] arrGeneral = generalTraits.Split(";");
            //SeedName = arrGeneral[0];
            Endurance = arrGeneral[1];
            SeedYield = arrGeneral[2];
            Lifespan = arrGeneral[3];
            Maturation = arrGeneral[4];
            GrowthStages = arrGeneral[5];
            SeedPotency = arrGeneral[6];
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
