using Robust.Shared.Serialization;

namespace Content.Shared._NF.PlantAnalyzer;

/// <summary>
///     The information about the last scanned plant/seed is stored here.
/// </summary>
[Serializable, NetSerializable]
public sealed class PlantAnalyzerScannedSeedPlantInformation : BoundUserInterfaceMessage
{
    public NetEntity? TargetEntity;
    public bool IsTray;

    public string? SeedName;
    public string[]? SeedChem;
    public AnalyzerHarvestType HarvestType;
    public GasFlags ExudeGases;
    public GasFlags ConsumeGases;
    public float Endurance;
    public int SeedYield;
    public float Lifespan;
    public float Maturation;
    public float Production;
    public int GrowthStages;
    public float SeedPotency;
    public string[]? Speciation; // Currently only available on server, we need to send strings to the client.
    public AdvancedScanInfo? AdvancedInfo;
}

/// <summary>
///     Information gathered in an advanced scan.
/// </summary>
[Serializable, NetSerializable]
public struct AdvancedScanInfo
{
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
    public MutationFlags Mutations;
}

// Note: currently leaving out Viable.
[Flags]
public enum MutationFlags : byte
{
    None = 0,
    TurnIntoKudzu = 1,
    Seedless = 2,
    Ligneous = 4,
    CanScream = 8,
}

[Flags]
public enum GasFlags : short
{
    None = 0,
    Nitrogen = 1,
    Oxygen = 2,
    CarbonDioxide = 4,
    Plasma = 8,
    Tritium = 16,
    WaterVapor = 32,
    Ammonia = 64,
    NitrousOxide = 128,
    Frezon = 256,
}

public enum AnalyzerHarvestType : byte
{
    Unknown, // Just in case the backing enum type changes and we haven't caught it.
    Repeat,
    NoRepeat,
    SelfHarvest
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
