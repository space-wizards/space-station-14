using System.Globalization;
using System.Linq;
using Content.Shared.Antag;
using Content.Shared.GameTicking.Components;
using Content.Shared.GameTicking.Rules.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Station.Systems;
using Content.Shared.Zombies;
using Robust.Shared.Player;

namespace Content.Shared.GameTicking.Rules;

public abstract partial class ZombieRuleSystem : GameRuleSystem<ZombieRuleComponent>
{
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedMindSystem _mindSystem = default!;
    [Dependency] private SharedRoleSystem _roles = default!;
    [Dependency] protected StationSystem Station = default!;

    [Dependency] private EntityQuery<ZombieComponent> _zombieQuery;

    protected override void Started(Entity<ZombieRuleComponent, GameRuleComponent> rule, ref GameRuleStartedEvent args)
    {
        base.Started(rule, ref args);

        rule.Comp1.NextRoundEndCheck = Timing.CurTime + rule.Comp1.EndCheckDelay;
    }

    [SubscribeLocalEvent]
    private void OnGetBriefing(Entity<InitialInfectedRoleComponent> role, ref GetBriefingEvent args)
    {
        if (!_roles.MindHasRole<ZombieRoleComponent>(args.Mind.Owner))
            args.Append(Loc.GetString("zombie-patientzero-role-greeting"));
    }

    [SubscribeLocalEvent]
    private void OnGetBriefing(Entity<ZombieRoleComponent> role, ref GetBriefingEvent args)
    {
        args.Append(Loc.GetString("zombie-infection-greeting"));
    }

    protected override void AppendRoundEndText(Entity<ZombieRuleComponent> rule,
        ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(rule, ref args);

        // This is just the general condition thing used for determining the win/lose text
        var fraction = GetInfectedFraction(true, true);

        if (fraction <= 0)
            args.AddLine(Loc.GetString("zombie-round-end-amount-none"));
        else if (fraction <= 0.25)
            args.AddLine(Loc.GetString("zombie-round-end-amount-low"));
        else if (fraction <= 0.5)
            args.AddLine(Loc.GetString("zombie-round-end-amount-medium", ("percent", Math.Round((fraction * 100), 2).ToString(CultureInfo.InvariantCulture))));
        else if (fraction < 1)
            args.AddLine(Loc.GetString("zombie-round-end-amount-high", ("percent", Math.Round((fraction * 100), 2).ToString(CultureInfo.InvariantCulture))));
        else
            args.AddLine(Loc.GetString("zombie-round-end-amount-all"));

        var antags = _antag.GetAntagIdentifiers(rule.Owner).ToList();
        args.AddLine(Loc.GetString("zombie-round-end-initial-count", ("initialCount", antags.Count)));
        foreach (var (_, data, entName) in antags)
        {
            args.AddLine(Loc.GetString("zombie-round-end-user-was-initial",
                ("name", entName),
                ("username", data.UserName)));
        }

        var healthy = GetHealthyHumans();
        // Gets a bunch of the living players and displays them if they're under a threshold.
        // InitialInfected is used for the threshold because it scales with the player count well.
        if (healthy.Count <= 0 || healthy.Count > 2 * antags.Count)
            return;
        args.AddLine("");
        args.AddLine(Loc.GetString("zombie-round-end-survivor-count", ("count", healthy.Count)));
        foreach (var survivor in healthy)
        {
            var meta = MetaData(survivor);
            var username = string.Empty;
            if (_mindSystem.TryGetMind(survivor, out _, out var mind) &&
                _player.TryGetSessionById(mind.UserId, out var session))
            {
                username = session.Name;
            }

            args.AddLine(Loc.GetString("zombie-round-end-user-was-survivor",
                ("name", meta.EntityName),
                ("username", username)));
        }
        args.AddLine("");
    }


    /// <summary>
    /// Get the fraction of players that are infected, between 0 and 1
    /// </summary>
    /// <param name="includeOffStation">Include healthy players that are not on the station grid</param>
    /// <param name="includeDead">Should dead zombies be included in the count</param>
    /// <returns></returns>
    protected float GetInfectedFraction(bool includeOffStation = true, bool includeDead = false)
    {
        var players = GetHealthyHumans(includeOffStation);
        var zombieCount = 0;
        var query = EntityQueryEnumerator<HumanoidProfileComponent, ZombieComponent, MobStateComponent>();
        while (query.MoveNext(out _, out _, out _, out var mob))
        {
            if (!includeDead && mob.CurrentState == MobState.Dead)
                continue;
            zombieCount++;
        }

        return zombieCount / (float) (players.Count + zombieCount);
    }

    /// <summary>
    /// Gets the list of humans who are alive, not zombies, and are on a station.
    /// Flying off via a shuttle disqualifies you.
    /// </summary>
    /// <returns></returns>
    protected List<EntityUid> GetHealthyHumans(bool includeOffStation = true)
    {
        var healthy = new List<EntityUid>();

        var stationGrids = new HashSet<EntityUid>();
        if (!includeOffStation)
        {
            foreach (var station in Station.GetStationsSet())
            {
                if (Station.GetLargestGrid(station) is { } grid)
                    stationGrids.Add(grid);
            }
        }

        var players = AllEntityQuery<HumanoidProfileComponent, ActorComponent, MobStateComponent, TransformComponent>();
        while (players.MoveNext(out var uid, out _, out _, out var mob, out var xform))
        {
            if (!_mobState.IsAlive(uid, mob))
                continue;

            if (_zombieQuery.HasComponent(uid))
                continue;

            if (!includeOffStation && !stationGrids.Contains(xform.GridUid ?? EntityUid.Invalid))
                continue;

            healthy.Add(uid);
        }
        return healthy;
    }
}
