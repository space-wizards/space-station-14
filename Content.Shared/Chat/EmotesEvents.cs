using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Chat;

[Serializable, NetSerializable]
public sealed class PlayEmoteMessage(ProtoId<EmotePrototype> protoId, string? customEmote) : EntityEventArgs
{
    public readonly ProtoId<EmotePrototype> ProtoId = protoId;
    public readonly string? CustomEmote = customEmote;
}
