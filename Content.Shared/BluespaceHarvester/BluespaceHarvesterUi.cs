using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.BluespaceHarvester;

[Serializable, NetSerializable]
public sealed class BluespaceHarvesterBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly int TargetLevel;
    public readonly int CurrentLevel;
    public readonly int MaxLevel;

    public readonly uint PowerUsage;
    public readonly uint PowerUsageNext;
    public readonly float PowerSuppliert;

    public readonly int Points;
    public readonly int TotalPoints;
    public readonly int PointsGen;

    public readonly List<BluespaceHarvesterCategoryInfo> Categories;

    public BluespaceHarvesterBoundUserInterfaceState(int targetLevel, int currentLevel, int maxLevel, uint powerUsage, uint powerUsageNext, float powerSuppliert, int points, int totalPoints, int pointsGen, List<BluespaceHarvesterCategoryInfo> categories)
    {
        TargetLevel = targetLevel;
        CurrentLevel = currentLevel;
        MaxLevel = maxLevel;

        PowerUsage = powerUsage;
        PowerUsageNext = powerUsageNext;
        PowerSuppliert = powerSuppliert;

        Points = points;
        TotalPoints = totalPoints;
        PointsGen = pointsGen;

        Categories = categories;
    }
}

[Serializable, NetSerializable]
public sealed class BluespaceHarvesterTargetLevelMessage : BoundUserInterfaceMessage
{
    public readonly int TargetLevel;

    public BluespaceHarvesterTargetLevelMessage(int targetLevel)
    {
        TargetLevel = targetLevel;
    }
}

[Serializable, NetSerializable]
public sealed class BluespaceHarvesterBuyMessage : BoundUserInterfaceMessage
{
    public readonly BluespaceHarvesterCategory Category;

    public BluespaceHarvesterBuyMessage(BluespaceHarvesterCategory category)
    {
        Category = category;
    }
}

[Serializable, NetSerializable]
public enum BluespaceHarvesterUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public enum BluespaceHarvesterVisuals : byte
{
    Tap0,
    Tap1,
    Tap2,
    Tap3,
    Tap4,
    Tap5,
    TapRedspace,
}

[Serializable, NetSerializable]
public enum BluespaceHarvesterVisualLayers : byte
{
    Base,
    Effects,
}

[Serializable, NetSerializable, DataDefinition]
public partial struct BluespaceHarvesterCategoryInfo
{
    [DataField("id")]
    public EntProtoId PrototypeId = "RandomHarvesterIndustrialLoot";

    [DataField]
    public int Cost = 0;

    [DataField]
    public BluespaceHarvesterCategory Type;
}


[Serializable, NetSerializable]
public enum BluespaceHarvesterCategory : byte
{
    Industrial,
    Technological,
    Biological,
    Destruction,
}
