using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Corvax.EmotesMenu;

[Serializable, NetSerializable]
public enum EmotesUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class PlayEmoteMessage(ProtoId<EmotePrototype> protoId) : BoundUserInterfaceMessage
{
    public readonly ProtoId<EmotePrototype> ProtoId = protoId;
}
