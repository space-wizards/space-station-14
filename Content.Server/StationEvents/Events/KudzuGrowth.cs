using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public class KudzuGrowth : StationEvent
{
    [Dependency] private IRobustRandom _robustRandom = default!;
    [Dependency] private IEntityManager _entityManager = default!;

    public override string Name => "KudzuGrowth";

    public override string? StartAnnouncement =>
        Loc.GetString("station-event-kudzu-growth-start-announcement");

    public override string? StartAudio => "/Audio/Announcements/bloblarm.ogg";

    public override int EarliestStart => 15;

    public override int MinimumPlayers => 15;

    public override float Weight => WeightLow;

    public override int? MaxOccurrences => 2;

    // Get players to scatter a bit looking for it.
    protected override float StartAfter => 50f;

    // Give crew at least 9 minutes to either have it gone, or to suffer another event. Kudzu is not actually required to be dead for another event to roll.
    protected override float EndAfter => 60*4;

    private EntityUid _targetGrid;

    private Vector2i _targetTile;

    private EntityCoordinates _targetCoords;

    public override void Startup()
    {
        base.Startup();

        // Pick a place to plant the kudzu.
        if (TryFindRandomTile(out _targetTile, out _, out _targetGrid, out _targetCoords, _robustRandom, _entityManager))
        {
            _entityManager.SpawnEntity("Kudzu", _targetCoords);
            Logger.InfoS("stationevents", $"Spawning a Kudzu at {_targetTile} on {_targetGrid}");
        }

        // If the kudzu tile selection fails we just let the announcement happen anyways because it's funny and people
        // will be hunting the non-existent, dangerous plant.
    }

}
