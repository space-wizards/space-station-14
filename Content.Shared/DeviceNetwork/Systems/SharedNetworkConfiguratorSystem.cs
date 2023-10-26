using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared.DeviceNetwork.Systems;

public abstract class SharedNetworkConfiguratorSystem : EntitySystem
{
}

public sealed partial class ClearAllOverlaysEvent : InstantActionEvent
{
}

[Serializable, NetSerializable]
public enum NetworkConfiguratorVisuals
{
    Mode
}

[Serializable, NetSerializable]
public enum NetworkConfiguratorLayers
{
    ModeLight
}
