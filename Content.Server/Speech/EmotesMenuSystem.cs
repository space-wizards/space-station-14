using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Speech;

public sealed partial class EmotesMenuSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllEvent<PlayEmoteMessage>(OnPlayEmote);
    }

    private void OnPlayEmote(PlayEmoteMessage msg, EntitySessionEventArgs args)
    {
        var player = args.SenderSession.AttachedEntity;
        if (!player.HasValue)
            return;

        if (!_prototypeManager.TryIndex(msg.ProtoId, out var proto))
            return;

        if (proto.IsCustomContentType)
        {
            _chat.TryCustomEmote(player.Value, proto, msg.CustomEmote);
        }
        else if (proto.ChatTriggers.Count > 0)
        {
            _chat.TryEmoteWithChat(player.Value, msg.ProtoId);
        }
    }
}
