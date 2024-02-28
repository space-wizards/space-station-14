using Robust.Shared.Serialization;

namespace Content.Shared.PlantAnalyzer;

/// <summary>
///     The information about the last scanned plant/seed is stored here.
/// </summary>
[Serializable, NetSerializable]
public sealed class PlantAnalyzerScannedSeedPlantInformation : BoundUserInterfaceMessage
{
    public readonly NetEntity? TargetEntity;
    public bool ScanMode;
    public bool IsTray;

    public string SeedName;
    public string SeedChem;
    public string HarvestType;
    public string ExudeGases;
    public float Endurance;
    public float SeedYield;
    public float Lifespan;
    public float Maturation;
    public float GrowthStages;
    public float SeedPotency;
    public float NutrientConsumption;
    public float WaterConsumption;
    public float IdealHeat;
    public float HeatTolerance;
    public float IdealLight;
    public float LightTolerance;
    public float ToxinsTolerance;
    public float LowPressureTolerance;
    public float HighPressureTolerance;
    public float PestTolerance;
    public float WeedTolerance;

    public string? MutationsList;
    public List<string>? Speciation;

    public PlantAnalyzerScannedSeedPlantInformation(NetEntity? targetEntity, bool scanMode, bool isTray,
        string seedName, string seedChem, string harvestType, string exudeGases, float endurance,
        float seedYield, float lifespan, float maturation, float growthStages, float seedPotency,
        float nutrientConsumption, float waterConsumption, float idealHeat, float heatTolerance,
        float idealLight, float lightTolerance, float toxinsTolerance, float lowPressureTolerance,
        float highPressureTolerance, float pestTolerance, float weedTolerance, string? mutationsList, List<string>? speciation)
    {
        TargetEntity = targetEntity;
        ScanMode = scanMode;
        IsTray = isTray;

        SeedName = seedName;
        SeedChem = seedChem;
        HarvestType = harvestType;
        ExudeGases = exudeGases;
        Endurance = endurance;
        SeedYield = seedYield;
        Lifespan = lifespan;
        Maturation = maturation;
        GrowthStages = growthStages;
        SeedPotency = seedPotency;

        if (scanMode)
        {
            NutrientConsumption = nutrientConsumption;
            WaterConsumption = waterConsumption;
            IdealHeat = idealHeat;
            HeatTolerance = heatTolerance;
            IdealLight = idealLight;
            LightTolerance = lightTolerance;
            ToxinsTolerance = toxinsTolerance;
            LowPressureTolerance = lowPressureTolerance;
            HighPressureTolerance = highPressureTolerance;
            PestTolerance = pestTolerance;
            WeedTolerance = weedTolerance;

            MutationsList = mutationsList;
            Speciation = speciation;
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
