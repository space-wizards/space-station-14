using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Server.Shuttles.Components;
using Content.Server.Singularity.Events;
using Content.Server.Singularity.PizzaLord.Components;

namespace Content.Server.Singularity.PizzaLord.Systems;

public sealed class AnnounceOnAbsorbSystem : EntitySystem
{   
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnnounceOnAbsorbComponent, EventHorizonConsumedEntityEvent>(OnAbsorb);
    }

    private void OnAbsorb(EntityUid uid, AnnounceOnAbsorbComponent comp, EventHorizonConsumedEntityEvent args)
    {
        var msg = Loc.GetString(comp.Text, ("object", Name(uid)));
        var title = Loc.GetString(comp.Title);
        _chatSystem.DispatchGlobalAnnouncement(msg, title, announcementSound: comp.AnnouncementSound, colorOverride: comp.AnnouncementColor);
    }
}
