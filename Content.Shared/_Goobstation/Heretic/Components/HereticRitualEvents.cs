using Content.Shared.Heretic.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Goobstation.Heretic.Components;

[Serializable, NetSerializable]
public sealed class HereticRitualMessage : BoundUserInterfaceMessage
{
    public ProtoId<HereticRitualPrototype> ProtoId;

    public HereticRitualMessage(ProtoId<HereticRitualPrototype> protoId)
    {
        ProtoId = protoId;
    }
}

[Serializable, NetSerializable]
public enum HereticRitualRuneUiKey : byte
{
    Key
}
