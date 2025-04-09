using Content.Shared.Teleportation.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Teleportation;

[Serializable, NetSerializable]
public enum TeleportLocationUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class TeleportLocationRequestPointsMessage() : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class TeleportLocationRequestTeleportMessage(NetEntity netEnt) : BoundUserInterfaceMessage
{
    public NetEntity NetEnt = netEnt;
}
