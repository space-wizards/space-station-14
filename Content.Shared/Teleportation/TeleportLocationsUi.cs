using Robust.Shared.Serialization;

namespace Content.Shared.Teleportation;

[Serializable, NetSerializable]
public enum TeleportLocationUiKey : byte
{
    Key
}

/// <summary>
/// Sends message to request available teleport points
/// </summary>
[Serializable, NetSerializable]
public sealed class TeleportLocationRequestPointsMessage : BoundUserInterfaceMessage
{
}

/// <summary>
/// Sends message to request that the clicker teleports to the requested location
/// </summary>
[Serializable, NetSerializable]
public sealed class TeleportLocationDestinationMessage(NetEntity netEnt, string pointName) : BoundUserInterfaceMessage
{
    public NetEntity NetEnt = netEnt;
    public string PointName = pointName;
}

[Serializable, NetSerializable]
public sealed partial class TeleportLocationUpdateState : BoundUserInterfaceState
{

}
