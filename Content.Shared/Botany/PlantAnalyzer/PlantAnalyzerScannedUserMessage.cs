using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Botany.PlantAnalyzer;

[Serializable, NetSerializable]
public sealed class PlantAnalyzerScannedUserMessage : BoundUserInterfaceMessage
{
    // TODO: PA
    public readonly NetEntity? TargetEntity;
    public bool? ScanMode;
    public PlantAnalyzerSeedData? SeedData;
    public PlantAnalyzerTrayData? TrayData;


    public PlantAnalyzerScannedUserMessage(NetEntity? targetEntity, bool? scanMode, PlantAnalyzerSeedData? seedData, PlantAnalyzerTrayData? trayData)
    {
        TargetEntity = targetEntity;
        ScanMode = scanMode;
        SeedData = seedData;
        TrayData = trayData;
    }
}

[Serializable, NetSerializable]
public sealed class PlantAnalyzerSeedData(string displayName)
{
    public string DisplayName = displayName;
}

[Serializable, NetSerializable]
public sealed class PlantAnalyzerTrayData(float waterLevel, float nutritionLevel, float toxins, float pestLevel, float weedLevel)
{
    public float WaterLevel = waterLevel;
    public float NutritionLevel = nutritionLevel;
    public float Toxins = toxins;
    public float PestLevel = pestLevel;
    public float WeedLevel = weedLevel;
}
