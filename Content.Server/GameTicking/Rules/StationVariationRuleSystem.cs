using Content.Server.GameTicking.Rules.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Events;
using Content.Server.Station.Systems;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// This handles starting various roundstart variation rules after a station has been loaded.
/// </summary>
public sealed class StationVariationRuleSystem : GameRuleSystem<StationVariationRuleComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationPostInitEvent>(OnStationPostInit);
    }

    protected override void Added(EntityUid uid, StationVariationRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        foreach (var rule in component.Rules)
        {
            GameTicker.AddGameRule(rule);
        }
    }

    private void OnStationPostInit(ref StationPostInitEvent ev)
    {
        // this is unlikely, but could happen if it was saved and reloaded, so check anyway
        if (HasComp<StationVariationHasRunComponent>(ev.Station))
            return;

        // raise the event on any passes that have been added
        var passEv = new StationVariationPassEvent(ev.Station);
        var passQuery = EntityQueryEnumerator<StationVariationPassRuleComponent, GameRuleComponent>();
        while (passQuery.MoveNext(out var uid, out _, out _))
        {
            RaiseLocalEvent(uid, ref passEv);
        }

        EnsureComp<StationVariationHasRunComponent>(ev.Station);
    }
}

/// <summary>
///     Raised directed on game rule entities which are added and marked as <see cref="StationVariationPassRuleComponent"/>
///     when a new station is initialized that should be variantized.
/// </summary>
/// <param name="Station">The new station that was added, and its config & grids.</param>
[ByRefEvent]
public record struct StationVariationPassEvent(Entity<StationDataComponent> Station);
