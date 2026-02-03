using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Ghost.Roles;

[Serializable, NetSerializable]
public sealed class GhostRoleRadioMessage : BoundUserInterfaceMessage
{
    public ProtoId<GhostRolePrototype> ProtoId;
    public NetEntity? Initiator;

    public GhostRoleRadioMessage(ProtoId<GhostRolePrototype> protoId, NetEntity? initiator)
    {
        ProtoId = protoId;
        Initiator = initiator;
    }
}

[Serializable, NetSerializable]
public enum GhostRoleRadioUiKey : byte
{
    Key
}
