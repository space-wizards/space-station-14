using Robust.Shared.Serialization;

namespace Content.Shared.Botany.PlantAnalyzer;

[Serializable, NetSerializable]
public sealed class PlantAnalyzerScannedUserMessage : BoundUserInterfaceMessage
{
    // TODO: PA
    public readonly NetEntity? TargetEntity;
    public bool? ScanMode;
    public PlantAnalyzerSeedData? SeedData;


    public PlantAnalyzerScannedUserMessage(NetEntity? targetEntity, bool? scanMode, PlantAnalyzerSeedData? seedData)
    {
        TargetEntity = targetEntity;
        ScanMode = scanMode;
        SeedData = seedData;
    }
}

[Serializable, NetSerializable]
public sealed class PlantAnalyzerSeedData(string displayName)
{
    public string DisplayName { get; private set; } = displayName;
}
