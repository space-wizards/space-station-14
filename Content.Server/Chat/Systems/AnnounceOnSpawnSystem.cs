using Content.Server.Chat;
using Content.Server.Chat.V2;

namespace Content.Server.Chat.Systems;

public sealed class AnnounceOnSpawnSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnnounceOnSpawnComponent, MapInitEvent>(OnInit);
    }

    private void OnInit(EntityUid uid, AnnounceOnSpawnComponent comp, MapInitEvent args)
    {
        _chat.DispatchGlobalAnnouncement(Loc.GetString(comp.Message), comp.Sender != null ? Loc.GetString(comp.Sender) : "Central Command", playSound: true, comp.Sound, comp.Color);
    }
}
