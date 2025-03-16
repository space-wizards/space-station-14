using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Ghost.Roles;

[Serializable, NetSerializable]
public sealed class GhostRoleRadioMessage : BoundUserInterfaceMessage
{
    public ProtoId<GhostRolePrototype> ProtoId;

    public GhostRoleRadioMessage(ProtoId<GhostRolePrototype> protoId)
    {
        ProtoId = protoId;
    }
}

[Serializable, NetSerializable]
public enum GhostRoleRadioUiKey : byte
{
    Key
}
