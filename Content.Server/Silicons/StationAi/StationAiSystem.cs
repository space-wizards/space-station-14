using Content.Server.Chat.Systems;
using Content.Shared.Silicons.StationAi;
using Content.Shared.StationAi;
using Robust.Shared.Player;

namespace Content.Server.Silicons.StationAi;

public sealed class StationAiSystem : SharedStationAiSystem
{
    [Dependency] private readonly ChatSystem _chats = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    private HashSet<Entity<StationAiCoreComponent>> _ais = new();

    public override bool SetEnabled(Entity<StationAiVisionComponent> entity, bool enabled, bool announce = false)
    {
        if (!base.SetEnabled(entity, enabled, announce))
            return false;

        var xform = Transform(entity);

        _ais.Clear();
        _lookup.GetChildEntities(xform.ParentUid, _ais);
        var filter = Filter.Empty();

        foreach (var ai in _ais)
        {
            // TODO: Filter API?
            if (TryComp(ai.Owner, out ActorComponent? actorComp))
            {
                filter.AddPlayer(actorComp.PlayerSession);
            }
        }

        _chats.DispatchFilteredAnnouncement(filter, Loc.GetString("ai-wire-snipped"), entity.Owner, announcementSound: null);
        return true;
    }
}
