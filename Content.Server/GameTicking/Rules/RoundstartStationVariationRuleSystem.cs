using System.Linq;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Components;
using Content.Server.Station.Events;
using Content.Shared.GameTicking.Components;
using Content.Shared.Storage;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.GameTicking.Rules;

/// <inheritdoc cref="RoundstartStationVariationRuleComponent"/>
public sealed class RoundstartStationVariationRuleSystem : GameRuleSystem<RoundstartStationVariationRuleComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationPostInitEvent>(OnStationPostInit, after: new []{typeof(ShuttleSystem)});
    }

    protected override void Added(EntityUid uid, RoundstartStationVariationRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        var spawns = EntitySpawnCollection.GetSpawns(component.Rules, _random);
        foreach (var rule in spawns)
        {
            GameTicker.AddGameRule(rule);
        }
    }

    private void OnStationPostInit(ref StationPostInitEvent ev)
    {
        // as long as one is running
        if (!GameTicker.IsGameRuleAdded<RoundstartStationVariationRuleComponent>())
            return;

        // this is unlikely, but could theoretically happen if it was saved and reloaded, so check anyway
        if (HasComp<StationVariationHasRunComponent>(ev.Station))
            return;

        Log.Info($"Running variation rules for station {ToPrettyString(ev.Station)}");

        // raise the event on any passes that have been added
        var passEv = new StationVariationPassEvent(ev.Station);
        var passQuery = EntityQueryEnumerator<StationVariationPassRuleComponent, GameRuleComponent>();
        while (passQuery.MoveNext(out var uid, out _, out _))
        {
            // TODO: for some reason, ending a game rule just gives it a marker comp,
            // and doesnt delete it
            // so we have to check here that it isnt an ended game rule (which could happen if a preset failed to start
            // or it was ended before station maps spawned etc etc etc)
            if (HasComp<EndedGameRuleComponent>(uid))
                continue;

            RaiseLocalEvent(uid, ref passEv);
        }

        EnsureComp<StationVariationHasRunComponent>(ev.Station);
    }
}

/// <summary>
///     Raised directed on game rule entities which are added and marked as <see cref="StationVariationPassRuleComponent"/>
///     when a new station is initialized that should be varied.
/// </summary>
/// <param name="Station">The new station that was added, and its config & grids.</param>
[ByRefEvent]
public readonly record struct StationVariationPassEvent(Entity<StationDataComponent> Station);
