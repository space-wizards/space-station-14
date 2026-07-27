using Content.Server.Antag;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Popups;
using Content.Server.Roles;
using Content.Server.RoundEnd;
using Content.Server.Station.Systems;
using Content.Server.Zombies;
using Content.Shared.GameTicking.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Administration;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Zombies;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using System.Globalization;
using Content.Server.Database;

namespace Content.Server.GameTicking.Rules;

public sealed class ZombieRuleSystem : GameRuleSystem<ZombieRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly IAdminManager _admin = default!; // DS14
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly ZombieSystem _zombie = default!;
    [Dependency] private readonly IServerDbManager _db = default!; // DS14

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InitialInfectedRoleComponent, GetBriefingEvent>(OnGetBriefing);
        SubscribeLocalEvent<ZombieRoleComponent, GetBriefingEvent>(OnGetBriefing);
        SubscribeLocalEvent<IncurableZombieComponent, ZombifySelfActionEvent>(OnZombifySelf);
    }

    private void OnGetBriefing(Entity<InitialInfectedRoleComponent> role, ref GetBriefingEvent args)
    {
        if (!_roles.MindHasRole<ZombieRoleComponent>(args.Mind.Owner))
            args.Append(Loc.GetString("zombie-patientzero-role-greeting"));
    }

    private void OnGetBriefing(Entity<ZombieRoleComponent> role, ref GetBriefingEvent args)
    {
        args.Append(Loc.GetString("zombie-infection-greeting"));
    }

    protected override void AppendRoundEndText(EntityUid uid,
        ZombieRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

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

        var antags = _antag.GetAntagIdentifiers(uid);
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

        // DS14-dashboard
        var winner = fraction > 0.9
            ? BiStatWinner.Antagonist
            : BiStatWinner.Crew;

        _ = System.Threading.Tasks.Task.Run(async () =>
        {
            try
            {
                await _db.AddBiStatAsync("Зомби", winner, DateTime.UtcNow);
            }
            catch
            {

            }
        });
    }

    // DS14-start
    protected override void AppendRoundEndDiscordText(EntityUid uid,
        ZombieRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndDiscordTextAppendEvent args)
    {
        var antags = _antag.GetAntagIdentifiers(uid);
        args.AddLine(Loc.GetString("zombie-round-end-initial-count", ("initialCount", antags.Count)));

        foreach (var (_, data, entName) in antags)
        {
            args.AddLine(Loc.GetString("zombie-round-end-user-was-initial",
                ("name", entName),
                ("username", data.UserName)));
        }

        args.AddLine("");
    }
    // DS14-end

    /// <summary>
    ///     The big kahoona function for checking if the round is gonna end
    /// </summary>
    private void CheckRoundEnd(ZombieRuleComponent zombieRuleComponent)
    {
        var healthy = GetHealthyHumans();
        if (healthy.Count == 1) // Only one human left. spooky
            _popup.PopupEntity(Loc.GetString("zombie-alone"), healthy[0], healthy[0]);

        var infectedFraction = GetInfectedFraction(false); // DS14

        // DS14-start
        if (!zombieRuleComponent.ZombieShuttleAutoCallHandled &&
            infectedFraction > zombieRuleComponent.ZombieShuttleCallPercentage)
        {
            zombieRuleComponent.ZombieShuttleAutoCallHandled = true;

            if (HasActiveRoundAdmin())
            {
                zombieRuleComponent.ZombieShuttleAutoCallDisabled = true;
            }
            else if (!_roundEnd.IsRoundEndRequested())
            {
                foreach (var station in _station.GetStations())
                {
                    _chat.DispatchStationAnnouncement(station, Loc.GetString("zombie-shuttle-call"), colorOverride: Color.Crimson);
                }
                _roundEnd.RequestRoundEnd(checkCooldown: false);
            }
        }
        // DS14-end

        // we include dead for this count because we don't want to end the round
        // when everyone gets on the shuttle.
        if (GetInfectedFraction() >= 1) // Oops, all zombies
            _roundEnd.EndRound();
    }

    // DS14-start
    private bool HasActiveRoundAdmin()
    {
        foreach (var admin in _admin.ActiveAdmins)
        {
            if (_admin.HasAdminFlag(admin, AdminFlags.Round))
                return true;
        }

        return false;
    }
    // DS14-end

    protected override void Started(EntityUid uid, ZombieRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        component.NextRoundEndCheck = _timing.CurTime + component.EndCheckDelay;
        // DS14-start
        component.ZombieShuttleAutoCallHandled = false;
        component.ZombieShuttleAutoCallDisabled = false;
        // DS14-end
    }

    protected override void ActiveTick(EntityUid uid, ZombieRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);
        if (!component.NextRoundEndCheck.HasValue || component.NextRoundEndCheck > _timing.CurTime)
            return;
        CheckRoundEnd(component);
        component.NextRoundEndCheck = _timing.CurTime + component.EndCheckDelay;
    }

    // DS14-start
    protected override void AppendAdminStatus(EntityUid uid,
        ZombieRuleComponent component,
        GameRuleComponent gameRule,
        CollectGameRuleAdminStatusEvent args)
    {
        var healthy = GetHealthyHumans(false).Count;
        var zombies = GetZombieCount(includeOffStation: false);
        var total = healthy + zombies;
        var infected = total == 0 ? 0f : zombies / (float) total;
        var shuttleStatus = component.ZombieShuttleAutoCallDisabled
            ? "game-rule-admin-status-zombie-shuttle-disabled"
            : component.ZombieShuttleAutoCallHandled
                ? "game-rule-admin-status-zombie-shuttle-handled"
                : "game-rule-admin-status-zombie-shuttle-pending";

        args.AddSection(
            Loc.GetString("game-rule-admin-status-zombie-title"),
            new[]
            {
                Loc.GetString(
                    "game-rule-admin-status-zombie-counts",
                    ("zombies", zombies),
                    ("healthy", healthy),
                    ("infected", infected.ToString("P0"))),
                Loc.GetString(
                    "game-rule-admin-status-zombie-shuttle",
                    ("status", Loc.GetString(shuttleStatus))),
            });
    }
    // DS14-end

    private void OnZombifySelf(EntityUid uid, IncurableZombieComponent component, ZombifySelfActionEvent args)
    {
        _zombie.ZombifyEntity(uid);
        if (component.Action != null)
            Del(component.Action.Value);
    }

    /// <summary>
    /// Get the fraction of players that are infected, between 0 and 1
    /// </summary>
    /// <param name="includeOffStation">Include healthy players that are not on the station grid</param>
    /// <param name="includeDead">Should dead zombies be included in the count</param>
    /// <returns></returns>
    private float GetInfectedFraction(bool includeOffStation = true, bool includeDead = false)
    {
        var players = GetHealthyHumans(includeOffStation);
        // DS14-start
        var zombieCount = GetZombieCount(includeDead, includeOffStation);
        var total = players.Count + zombieCount;

        return total == 0 ? 0f : zombieCount / (float) total;
    }

    private int GetZombieCount(bool includeDead = false, bool includeOffStation = true)
    {
        var zombieCount = 0;
        var stationGrids = new HashSet<EntityUid>();
        if (!includeOffStation)
        {
            foreach (var station in _station.GetStationsSet())
            {
                if (_station.GetLargestGrid(station) is { } grid)
                    stationGrids.Add(grid);
            }
        }

        var query = EntityQueryEnumerator<HumanoidAppearanceComponent, ZombieComponent, MobStateComponent, TransformComponent>();
        while (query.MoveNext(out _, out _, out _, out var mob, out var xform))
        {
            if (!includeDead && mob.CurrentState == MobState.Dead)
                continue;

            if (!includeOffStation && !stationGrids.Contains(xform.GridUid ?? EntityUid.Invalid))
                continue;

            zombieCount++;
        }

        return zombieCount;
    }
    // DS14-end

    /// <summary>
    /// Gets the list of humans who are alive, not zombies, and are on a station.
    /// Flying off via a shuttle disqualifies you.
    /// </summary>
    /// <returns></returns>
    private List<EntityUid> GetHealthyHumans(bool includeOffStation = true)
    {
        var healthy = new List<EntityUid>();

        var stationGrids = new HashSet<EntityUid>();
        if (!includeOffStation)
        {
            foreach (var station in _station.GetStationsSet())
            {
                if (_station.GetLargestGrid(station) is { } grid)
                    stationGrids.Add(grid);
            }
        }

        var players = AllEntityQuery<HumanoidAppearanceComponent, ActorComponent, MobStateComponent, TransformComponent>();
        var zombers = GetEntityQuery<ZombieComponent>();
        while (players.MoveNext(out var uid, out _, out _, out var mob, out var xform))
        {
            if (!_mobState.IsAlive(uid, mob))
                continue;

            if (zombers.HasComponent(uid))
                continue;

            if (!includeOffStation && !stationGrids.Contains(xform.GridUid ?? EntityUid.Invalid))
                continue;

            healthy.Add(uid);
        }
        return healthy;
    }
}
