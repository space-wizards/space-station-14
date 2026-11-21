using Content.Server.Nuke;
using Content.Server.RoundEnd;
using Content.Server.Station.Systems;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Nuke;

namespace Content.Server.StationEvents.Events;

public sealed partial class SuddenNukeArmRule : StationEventSystem<SuddenNukeArmRuleComponent>
{
    [Dependency] private NukeSystem _nukeSystem = default!;
    [Dependency] private RoundEndSystem _roundEndSystem = default!;
    [Dependency] private ServerStationSystem _stationSystem = default!;

    private bool IsNukePicked(out HashSet<EntityUid> pickedNukes)
    {
        pickedNukes = [];

        var query = EntityQueryEnumerator<SuddenNukeArmRuleComponent>();
        while (query.MoveNext(out _, out var suddenNukeArmRuleComponent))
        {
            if (suddenNukeArmRuleComponent.PickedNuke is not null)
            {
                pickedNukes.Add(suddenNukeArmRuleComponent.PickedNuke.Value);
            }
        }

        return pickedNukes.Count > 0;
    }

    protected override void Started(Entity<SuddenNukeArmRuleComponent, GameRuleComponent> rule,
        ref GameRuleStartedEvent args)
    {
        var nukes = _stationSystem.GetEntitiesWithComponentOnStation<NukeComponent>(false);

        foreach (var nuke in nukes)
        {
            if (IsNukePicked(out var existingPickedNukes)
                && existingPickedNukes.Contains(nuke.Owner))
            {
                continue;
            }

            if (nuke.Comp.Status == NukeStatus.ARMED)
            {
                continue;
            }

            // If nuke was already armed by other causes and then disarmed,
            // start counter from beginning again to give leeway.
            _nukeSystem.SetRemainingTime(nuke.Owner, nuke.Comp.Timer);

            _nukeSystem.ArmBomb(nuke.Owner, nuke.Comp);

            rule.Comp1.PickedNuke = nuke.Owner;
            break;
        }
    }


    [SubscribeLocalEvent]
    private void OnNukeExploded(NukeExplodedEvent ev)
    {
        if (!IsNukePicked(out var pickedNukes) || !pickedNukes.Contains(ev.ExplodedNuke))
        {
            return;
        }

        var query = EntityQueryEnumerator<SuddenNukeArmRuleComponent>();

        while (query.MoveNext(out _, out var component))
        {
            if (component.PickedNuke == ev.ExplodedNuke)
            {
                component.ExplodedNuke = ev.ExplodedNuke;
            }
        }

        _roundEndSystem.EndRound();
    }

    [SubscribeLocalEvent]
    private void OnNukeDisarm(NukeDisarmSuccessEvent ev)
    {
        var query = EntityQueryEnumerator<SuddenNukeArmRuleComponent>();

        while (query.MoveNext(out _, out var suddenNukeArmRuleComponent))
        {
            if (ev.DisarmedNuke == suddenNukeArmRuleComponent.PickedNuke)
            {
                suddenNukeArmRuleComponent.PickedNuke = null;
                break;
            }
        }
    }

    protected override void AppendRoundEndText(Entity<SuddenNukeArmRuleComponent> rule,
        ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(rule, ref args);

        if (rule.Comp.ExplodedNuke is null || rule.Comp.PickedNuke != rule.Comp.ExplodedNuke)
        {
            return;
        }

        args.AddLine(Loc.GetString("sudden-nuke-arm-event-end-round-nuke-exploded"));
        args.AddLine("");
    }
}
