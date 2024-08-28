using Content.Server.Chat;
using Content.Server.Announcements.Systems;
using Robust.Shared.Player;

namespace Content.Server.Chat.Systems;

public sealed class AnnounceOnSpawnSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly AnnouncerSystem _announcer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnnounceOnSpawnComponent, MapInitEvent>(OnInit);
    }

    private void OnInit(EntityUid uid, AnnounceOnSpawnComponent comp, MapInitEvent args)
    {
        var sender = comp.Sender != null ? Loc.GetString(comp.Sender) : Loc.GetString("chat-manager-sender-announcement");
        _announcer.SendAnnouncement(_announcer.GetAnnouncementId("SpawnAnnounceCaptain"), Filter.Broadcast(),
            comp.Message, sender, comp.Color);
    }
}
