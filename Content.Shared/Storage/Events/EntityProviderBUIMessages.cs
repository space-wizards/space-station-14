using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Storage.Events;

/// <summary>
/// This message is sent from the client when the player wants to switch the active entity type.
/// </summary>
/// <param name="entityProtoId">The entityProtoId to switch to.</param>
[Serializable, NetSerializable]
public sealed class SwitchSelectedEntity(EntProtoId entityProtoId) : BoundUserInterfaceMessage
{
    public EntProtoId EntityProtoId = entityProtoId;
}

/// <summary>
/// This message is sent from the client when the player wants to eject all kinds of the currently active entity.
/// </summary>
/// <param name="entityProtoId">The entityProtoId to eject.</param>
[Serializable, NetSerializable]
public sealed class EjectSelectedEntities(EntProtoId entityProtoId) : BoundUserInterfaceMessage
{
    public EntProtoId EntityProtoId = entityProtoId;
}
