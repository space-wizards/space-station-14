using Content.Shared.GameTicking;

namespace Content.Server.Polymorph.Systems;

public sealed partial class PolymorphSystem
{
    public EntityUid? PausedMap { get; private set; }

    /// <summary>
    /// Used to subscribe to the round restart event
    /// </summary>
    private void InitializeMap()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent _)
    {
        if (PausedMap == null || !Exists(PausedMap))
            return;

        Del(PausedMap.Value);
    }

    /// <summary>
    /// Used internally to ensure a paused map that is
    /// stores polymorphed entities.
    /// </summary>
    private EntityUid EnsurePausedMap()
    {
        if (PausedMap != null && Exists(PausedMap))
            return PausedMap.Value;

        var mapUid = _map.CreateMap();
        _metaData.SetEntityName(mapUid, Loc.GetString("polymorph-paused-map-name"));
        _map.SetPaused(mapUid, true);
        PausedMap = mapUid;

        return mapUid;
    }
}
