using Content.Server.GameTicking;
using Content.Server.Nuke;
using Content.Server.RoundEnd;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;

namespace Content.Server.StationEvents.Events;

public sealed partial class SuddenNukeArmRule : StationEventSystem<SuddenNukeArmRuleComponent>
{
    [Dependency] private NukeSystem _nukeSystem = default!;
    [Dependency] private RoundEndSystem _roundEndSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NukeExplodedEvent>(OnNukeExploded);
        SubscribeLocalEvent<NukeDisarmSuccessEvent>(OnNukeDisarm);
    }

    protected override void Started(EntityUid uid,
        SuddenNukeArmRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        if (!TryGetRandomStation(out var chosenStation))
            return;

        if (!TryComp<StationDataComponent>(chosenStation, out var stationData))
            return;

        var grid = StationSystem.GetLargestGrid((chosenStation.Value, stationData));

        if (grid is null)
            return;

        var query = EntityQueryEnumerator<NukeComponent>();

        while (query.MoveNext(out var nukeUid, out var nukeComponent))
        {
            if (Transform(nukeUid).ParentUid != grid)
            {
                continue;
            }

            // If nuke was already armed by other causes and then disarmed,
            // start counter from beginning again to give leeway.
            _nukeSystem.SetRemainingTime(nukeUid, nukeComponent.Timer);

            _nukeSystem.ArmBomb(nukeUid, nukeComponent);

            component.PickedNuke = nukeUid;
            break;
        }
    }

    private void OnNukeExploded(NukeExplodedEvent ev)
    {
        var query = EntityQueryEnumerator<SuddenNukeArmRuleComponent>();

        while (query.MoveNext(out _, out var suddenNukeArmRuleComponent))
        {
            if (suddenNukeArmRuleComponent.PickedNuke is null)
            {
                continue;
            }

            _roundEndSystem.EndRound();
            break;
        }
    }

    private void OnNukeDisarm(NukeDisarmSuccessEvent ev)
    {
        var query = EntityQueryEnumerator<SuddenNukeArmRuleComponent>();

        while (query.MoveNext(out _, out var suddenNukeArmRuleComponent))
        {
            suddenNukeArmRuleComponent.PickedNuke = null;
        }
    }

    protected override void AppendRoundEndText(EntityUid uid,
        SuddenNukeArmRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

        if (component.PickedNuke is null)
        {
            return;
        }

        args.AddLine(Loc.GetString("sudden-nuke-arm-event-end-round-nuke-exploded"));
        args.AddLine("");
    }
}
