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

    public BluespaceHarvesterBoundUserInterfaceState(int targetLevel, int currentLevel, int maxLevel, uint powerUsage, uint powerUsageNext)
    {
        TargetLevel = targetLevel;
        CurrentLevel = currentLevel;
        MaxLevel = maxLevel;

        PowerUsage = powerUsage;
        PowerUsageNext = powerUsageNext;
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
