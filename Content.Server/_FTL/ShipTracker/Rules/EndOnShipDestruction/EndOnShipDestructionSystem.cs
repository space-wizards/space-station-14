using System.Linq;
using Content.Server._FTL.FTLPoints;
using Content.Server._FTL.FTLPoints.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.RoundEnd;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;

namespace Content.Server._FTL.ShipTracker.Rules.EndOnShipDestruction;

/// <summary>
/// Manages <see cref="EndOnShipDestructionComponent"/>
/// </summary>
public sealed class EndOnShipDestructionSystem : GameRuleSystem<EndOnShipDestructionComponent>
{
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly FTLPointsSystem _pointsSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
        SubscribeLocalEvent<PlayerSpawningEvent>(OnPlayerSpawningEvent);
    }

    private void OnPlayerSpawningEvent(PlayerSpawningEvent ev)
    {
        var query = EntityQueryEnumerator<EndOnShipDestructionComponent, GameRuleComponent>();

        while (query.MoveNext(out var eosComponent, out _))
        {
            if (_configurationManager.GetCVar(CCVars.GenerateFTLPointsRoundstart))
            {
                _pointsSystem.RegeneratePoints();
            }

            if (!ev.Station.HasValue)
            {
                Log.Fatal("Unable to get station on player spawning, does it exist?");
                _roundEndSystem.EndRound();
                return;
            }

            if (!TryComp<StationDataComponent>(ev.Station.Value, out var stationDataComponent))
            {
                Log.Fatal("Failed to get station data!");
                _roundEndSystem.EndRound();
                return;
            }

            var entity = _stationSystem.GetLargestGrid(stationDataComponent);
            if (!entity.HasValue)
            {
                Log.Fatal("Failed to get largest station grid!");
                _roundEndSystem.EndRound();
                return;
            }

            eosComponent.MainShip = entity.Value;
        }
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        ev.AddLine(Loc.GetString("ftl-gamerule-end-text"));
    }

    protected override void ActiveTick(EntityUid uid, EndOnShipDestructionComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        if (!TryComp<ShipTrackerComponent>(component.MainShip, out var trackerComponent))
        {
            _roundEndSystem.EndRound();
            return;
        }

        if (trackerComponent.HullAmount <= 0)
            _roundEndSystem.EndRound();

    }
}
