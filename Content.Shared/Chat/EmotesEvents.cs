using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Chat;

[Serializable, NetSerializable]
public sealed class PlayEmoteMessage(ProtoId<EmotePrototype> protoId) : EntityEventArgs
{
    public readonly ProtoId<EmotePrototype> ProtoId = protoId;
}
