using System.Globalization;
using System.Linq;
using Content.Server.Actions;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Popups;
using Content.Server.Preferences.Managers;
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
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Zombies;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.GameTicking.Rules;

public sealed class ZombieRuleSystem : GameRuleSystem<ZombieRuleComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly ZombieSystem _zombie = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
        SubscribeLocalEvent<PendingZombieComponent, ZombifySelfActionEvent>(OnZombifySelf);
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
    private void CheckRoundEnd()
    {
        var query = EntityQueryEnumerator<ZombieRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var comp, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                continue;

            var healthy = GetHealthyHumans();
            if (healthy.Count == 1) // Only one human left. spooky
                _popup.PopupEntity(Loc.GetString("zombie-alone"), healthy[0], healthy[0]);

            if (!comp.ShuttleCalled && GetInfectedFraction(false) >= comp.ZombieShuttleCallPercentage)
            {
                comp.ShuttleCalled = true;
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
    }

    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        var query = EntityQueryEnumerator<ZombieRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out _, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            var minPlayers = _cfg.GetCVar(CCVars.ZombieMinPlayers);
            if (!ev.Forced && ev.Players.Length < minPlayers)
            {
                _chatManager.SendAdminAnnouncement(Loc.GetString("zombie-not-enough-ready-players",
                    ("readyPlayersCount", ev.Players.Length),
                    ("minimumPlayers", minPlayers)));
                ev.Cancel();
                continue;
            }

            if (ev.Players.Length == 0)
            {
                _chatManager.DispatchServerAnnouncement(Loc.GetString("zombie-no-one-ready"));
                ev.Cancel();
            }
        }
    }

    protected override void Started(EntityUid uid, ZombieRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
        component.StartTime = _timing.CurTime + _random.Next(component.MinStartDelay, component.MaxStartDelay);
    }

    protected override void ActiveTick(EntityUid uid, ZombieRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        if (component.InfectedChosen)
        {
            if (_timing.CurTime >= component.NextRoundEndCheck)
            {
                component.NextRoundEndCheck += component.EndCheckDelay;
                CheckRoundEnd();
            }
            return;
        }

        if (component.StartTime == null || _timing.CurTime < component.StartTime)
            return;

        InfectInitialPlayers(component);
    }

    private void OnZombifySelf(EntityUid uid, PendingZombieComponent component, ZombifySelfActionEvent args)
    {
        _zombie.ZombifyEntity(uid);
        if (component.Action != null)
            Del(component.Action.Value);
    }

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
        if (component.InfectedChosen)
            return;
        component.InfectedChosen = true;

        var allPlayers = _playerManager.Sessions.ToList();
        var playerList = new List<ICommonSession>();
        var prefList = new List<ICommonSession>();
        foreach (var player in allPlayers)
        {
            if (player.AttachedEntity == null || !HasComp<HumanoidAppearanceComponent>(player.AttachedEntity) || HasComp<ZombieImmuneComponent>(player.AttachedEntity))
                continue;
            playerList.Add(player);

            var pref = (HumanoidCharacterProfile) _prefs.GetPreferences(player.UserId).SelectedCharacter;
            if (pref.AntagPreferences.Contains(component.PatientZeroPrototypeId))
                prefList.Add(player);
        }

        if (playerList.Count == 0)
            return;

        var numInfected = Math.Max(1,
            (int) Math.Min(
                Math.Floor((double) playerList.Count / component.PlayersPerInfected), component.MaxInitialInfected));

        var totalInfected = 0;
        while (totalInfected < numInfected)
        {
            ICommonSession zombie;
            if (prefList.Count == 0)
            {
                if (playerList.Count == 0)
                {
                    Log.Info("Insufficient number of players. stopping selection.");
                    break;
                }
                zombie = _random.Pick(playerList);
                Log.Info("Insufficient preferred patient 0, picking at random.");
            }
            else
            {
                zombie = _random.Pick(prefList);
                Log.Info("Selected a patient 0.");
            }

            prefList.Remove(zombie);
            playerList.Remove(zombie);
            if (!_mindSystem.TryGetMind(zombie, out var mindId, out var mind) ||
                mind.OwnedEntity is not { } ownedEntity)
            {
                continue;
            }

            totalInfected++;

            _roles.MindAddRole(mindId, new InitialInfectedRoleComponent { PrototypeId = component.PatientZeroPrototypeId });

            var pending = EnsureComp<PendingZombieComponent>(ownedEntity);
            pending.GracePeriod = _random.Next(component.MinInitialInfectedGrace, component.MaxInitialInfectedGrace);
            EnsureComp<ZombifyOnDeathComponent>(ownedEntity);
            EnsureComp<IncurableZombieComponent>(ownedEntity);
            var inCharacterName = MetaData(ownedEntity).EntityName;
            _action.AddAction(ownedEntity, ref pending.Action, ZombieRuleComponent.ZombifySelfActionPrototype, ownedEntity);

            var message = Loc.GetString("zombie-patientzero-role-greeting");
            var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));

            //gets the names now in case the players leave.
            //this gets unhappy if people with the same name get chosen. Probably shouldn't happen.
            component.InitialInfectedNames.Add(inCharacterName, zombie.Name);

            // I went all the way to ChatManager.cs and all i got was this lousy T-shirt
            // You got a free T-shirt!?!?
            _chatManager.ChatMessageToOne(Shared.Chat.ChatChannel.Server, message,
               wrappedMessage, default, false, zombie.ConnectedClient, Color.Plum);
            _audio.PlayGlobal(component.InitialInfectedSound, ownedEntity);
        }
    }
}
