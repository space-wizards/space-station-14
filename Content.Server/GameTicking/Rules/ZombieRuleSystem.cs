using Content.Server.Actions;
using Content.Server.Antag;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Popups;
using Content.Server.Roles;
using Content.Server.RoundEnd;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Server.Zombies;
using Content.Shared.CCVar;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Roles;
using Content.Shared.Zombies;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Globalization;

namespace Content.Server.GameTicking.Rules;

public sealed class ZombieRuleSystem : GameRuleSystem<ZombieRuleComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly ZombieSystem _zombie = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly AntagSelectionSystem _antagSelection = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
        SubscribeLocalEvent<PendingZombieComponent, ZombifySelfActionEvent>(OnZombifySelf);
    }

    /// <summary>
    /// Set the required minimum players for this gamemode to start
    /// </summary>
    protected override void Added(EntityUid uid, ZombieRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        gameRule.MinPlayers = _cfg.GetCVar(CCVars.ZombieMinPlayers);
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        foreach (var zombie in EntityQuery<ZombieRuleComponent>())
        {
            // This is just the general condition thing used for determining the win/lose text
            var fraction = GetInfectedFraction(true, true);

            if (fraction <= 0)
                ev.AddLine(Loc.GetString("zombie-round-end-amount-none"));
            else if (fraction <= 0.25)
                ev.AddLine(Loc.GetString("zombie-round-end-amount-low"));
            else if (fraction <= 0.5)
                ev.AddLine(Loc.GetString("zombie-round-end-amount-medium", ("percent", Math.Round((fraction * 100), 2).ToString(CultureInfo.InvariantCulture))));
            else if (fraction < 1)
                ev.AddLine(Loc.GetString("zombie-round-end-amount-high", ("percent", Math.Round((fraction * 100), 2).ToString(CultureInfo.InvariantCulture))));
            else
                ev.AddLine(Loc.GetString("zombie-round-end-amount-all"));

            ev.AddLine(Loc.GetString("zombie-round-end-initial-count", ("initialCount", zombie.InitialInfectedNames.Count)));
            foreach (var player in zombie.InitialInfectedNames)
            {
                ev.AddLine(Loc.GetString("zombie-round-end-user-was-initial",
                    ("name", player.Key),
                    ("username", player.Value)));
            }

            var healthy = GetHealthyHumans();
            // Gets a bunch of the living players and displays them if they're under a threshold.
            // InitialInfected is used for the threshold because it scales with the player count well.
            if (healthy.Count <= 0 || healthy.Count > 2 * zombie.InitialInfectedNames.Count)
                continue;
            ev.AddLine("");
            ev.AddLine(Loc.GetString("zombie-round-end-survivor-count", ("count", healthy.Count)));
            foreach (var survivor in healthy)
            {
                var meta = MetaData(survivor);
                var username = string.Empty;
                if (_mindSystem.TryGetMind(survivor, out _, out var mind) && mind.Session != null)
                {
                    username = mind.Session.Name;
                }

                ev.AddLine(Loc.GetString("zombie-round-end-user-was-survivor",
                    ("name", meta.EntityName),
                    ("username", username)));
            }
        }
    }

    /// <summary>
    ///     The big kahoona function for checking if the round is gonna end
    /// </summary>
    private void CheckRoundEnd(ZombieRuleComponent zombieRuleComponent)
    {
        var healthy = GetHealthyHumans();
        if (healthy.Count == 1) // Only one human left. spooky
            _popup.PopupEntity(Loc.GetString("zombie-alone"), healthy[0], healthy[0]);

        if (GetInfectedFraction(false) > zombieRuleComponent.ZombieShuttleCallPercentage && !_roundEnd.IsRoundEndRequested())
        {
            foreach (var station in _station.GetStations())
            {
                _chat.DispatchStationAnnouncement(station, Loc.GetString("zombie-shuttle-call"), colorOverride: Color.Crimson);
            }
            _roundEnd.RequestRoundEnd(null, false);
        }

        // we include dead for this count because we don't want to end the round
        // when everyone gets on the shuttle.
        if (GetInfectedFraction() >= 1) // Oops, all zombies
            _roundEnd.EndRound();
    }

    /// <summary>
    /// Check we have enough players to start this game mode, if not - cancel and announce
    /// </summary>
    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        TryRoundStartAttempt(ev, Loc.GetString("zombie-title"));
    }

    protected override void Started(EntityUid uid, ZombieRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var delay = _random.Next(component.MinStartDelay, component.MaxStartDelay);
        component.StartTime = _timing.CurTime + delay;
    }

    protected override void ActiveTick(EntityUid uid, ZombieRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        if (component.StartTime.HasValue && component.StartTime < _timing.CurTime)
        {
            InfectInitialPlayers(component);
            component.StartTime = null;
            component.NextRoundEndCheck = _timing.CurTime + component.EndCheckDelay;
        }

        if (component.NextRoundEndCheck.HasValue && component.NextRoundEndCheck < _timing.CurTime)
        {
            CheckRoundEnd(component);
            component.NextRoundEndCheck = _timing.CurTime + component.EndCheckDelay;
        }
    }

    private void OnZombifySelf(EntityUid uid, PendingZombieComponent component, ZombifySelfActionEvent args)
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
        var zombieCount = 0;
        var query = EntityQueryEnumerator<HumanoidAppearanceComponent, ZombieComponent, MobStateComponent>();
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
    private List<EntityUid> GetHealthyHumans(bool includeOffStation = true)
    {
        var healthy = new List<EntityUid>();

        var stationGrids = new HashSet<EntityUid>();
        if (!includeOffStation)
        {
            foreach (var station in _station.GetStationsSet())
            {
                if (TryComp<StationDataComponent>(station, out var data) && _station.GetLargestGrid(data) is { } grid)
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

    /// <summary>
    ///     Infects the first players with the passive zombie virus.
    ///     Also records their names for the end of round screen.
    /// </summary>
    /// <remarks>
    ///     The reason this code is written separately is to facilitate
    ///     allowing this gamemode to be started midround. As such, it doesn't need
    ///     any information besides just running.
    /// </remarks>
    private void InfectInitialPlayers(ZombieRuleComponent component)
    {
        //Get all players with initial infected enabled, and exclude those with the ZombieImmuneComponent and roles with CanBeAntag = False
        var eligiblePlayers = _antagSelection.GetEligiblePlayers(
            _playerManager.Sessions,
            component.PatientZeroPrototypeId,
            includeAllJobs: false,
            customExcludeCondition: player => HasComp<ZombieImmuneComponent>(player) || HasComp<InitialInfectedExemptComponent>(player) 
            );

        //And get all players, excluding ZombieImmune and roles with CanBeAntag = False - to fill any leftover initial infected slots
        var allPlayers = _antagSelection.GetEligiblePlayers(
            _playerManager.Sessions,
            component.PatientZeroPrototypeId,
            acceptableAntags: Shared.Antag.AntagAcceptability.All,
            includeAllJobs: false ,
            ignorePreferences: true,
            customExcludeCondition: HasComp<ZombieImmuneComponent> 
            );

        //If there are no players to choose, abort
        if (allPlayers.Count == 0)
            return;

        //How many initial infected should we select
        var initialInfectedCount = _antagSelection.CalculateAntagCount(_playerManager.PlayerCount, component.PlayersPerInfected, component.MaxInitialInfected);

        //Choose the required number of initial infected from the eligible players, making up any shortfall by choosing from all players
        var initialInfected = _antagSelection.ChooseAntags(initialInfectedCount, eligiblePlayers, allPlayers);

        //Make brain craving
        MakeZombie(initialInfected, component);

        //Send the briefing, play greeting sound
        _antagSelection.SendBriefing(initialInfected, Loc.GetString("zombie-patientzero-role-greeting"), Color.Plum, component.InitialInfectedSound);
    }

    private void MakeZombie(List<EntityUid> entities, ZombieRuleComponent component)
    {
        foreach (var entity in entities)
        {
            MakeZombie(entity, component);
        }
    }
    private void MakeZombie(EntityUid entity, ZombieRuleComponent component)
    {
        if (!_mindSystem.TryGetMind(entity, out var mind, out var mindComponent))
            return;

        //Add the role to the mind silently (to avoid repeating job assignment)
        _roles.MindAddRole(mind, new InitialInfectedRoleComponent { PrototypeId = component.PatientZeroPrototypeId }, silent: true);

        //Add the zombie components and grace period
        var pending = EnsureComp<PendingZombieComponent>(entity);
        pending.GracePeriod = _random.Next(component.MinInitialInfectedGrace, component.MaxInitialInfectedGrace);
        EnsureComp<ZombifyOnDeathComponent>(entity);
        EnsureComp<IncurableZombieComponent>(entity);

        //Add the zombify action
        _action.AddAction(entity, ref pending.Action, component.ZombifySelfActionPrototype, entity);

        //Get names for the round end screen, incase they leave mid-round
        var inCharacterName = MetaData(entity).EntityName;
        var accountName = mindComponent.Session == null ? string.Empty : mindComponent.Session.Name;
        component.InitialInfectedNames.Add(inCharacterName, accountName);
    }
}
