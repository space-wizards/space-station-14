using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Robust.Shared.Prototypes;

namespace Content.Server.Corvax.EmotesMenu;

public sealed partial class EmotesMenuSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayEmoteMessage>(OnPlayEmote);
    }

    private void OnPlayEmote(PlayEmoteMessage ev)
    {
        var player = ev.Session.AttachedEntity;
        if (!player.HasValue)
            return;

        if (!_prototypeManager.TryIndex(ev.ProtoId, out var proto) || proto.ChatTriggers.Count == 0)
            return;

        _chat.TryEmoteWithChat(player.Value, ev.ProtoId);
    }
}
