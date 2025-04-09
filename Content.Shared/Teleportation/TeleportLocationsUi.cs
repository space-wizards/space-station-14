using Robust.Shared.Serialization;

namespace Content.Shared.Teleportation;

[Serializable, NetSerializable]
public enum TeleportLocationUiKey : byte
{
    Key
}

/// <summary>
///     Sends message to request available teleport points
/// </summary>
[Serializable, NetSerializable]
public sealed class TeleportLocationRequestPointsMessage : BoundUserInterfaceMessage
{
}

/// <summary>
///     Sends message to request that the clicker teleports to the requested location
/// </summary>
/// <param name="netEnt"></param>
/// <param name="pointName"></param>
[Serializable, NetSerializable]
public sealed class TeleportLocationRequestTeleportMessage(NetEntity netEnt, string pointName) : BoundUserInterfaceMessage
{
    public NetEntity NetEnt = netEnt;
    public string PointName = pointName;
}

/// <summary>
///     Sends a message to request that the BUI closes
/// </summary>
[Serializable, NetSerializable]
public sealed class TeleportLocationRequestCloseMessage : BoundUserInterfaceMessage
{

}
