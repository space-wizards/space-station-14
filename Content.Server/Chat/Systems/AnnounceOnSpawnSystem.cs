using Content.Server.Chat;

namespace Content.Server.Chat.Systems;

public sealed partial class AnnounceOnSpawnSystem : EntitySystem
{
    [Dependency] private ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnnounceOnSpawnComponent, MapInitEvent>(OnInit);
    }

    private void OnInit(EntityUid uid, AnnounceOnSpawnComponent comp, MapInitEvent args)
    {
        var message = Loc.GetString(comp.Message);
        var sender = comp.Sender != null ? Loc.GetString(comp.Sender) : Loc.GetString("chat-manager-sender-announcement");
        _chat.DispatchGlobalAnnouncement(message, sender, playSound: true, comp.Sound, comp.Color);
    }
}
