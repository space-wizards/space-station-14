using System.Linq;
using Content.Server.Actions;
using Content.Server.Chat.Managers;
using Content.Server.Disease;
using Content.Server.Disease.Components;
using Content.Server.Mind.Components;
using Content.Server.MobState;
using Content.Server.Players;
using Content.Server.Popups;
using Content.Server.Preferences.Managers;
using Content.Server.RoundEnd;
using Content.Server.Traitor;
using Content.Server.Zombies;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.CCVar;
using Content.Shared.Humanoid;
using Content.Shared.MobState;
using Content.Shared.MobState.Components;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Zombies;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking.Rules;

public sealed class ZombieRuleSystem : GameRuleSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly DiseaseSystem _diseaseSystem = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly ZombifyOnDeathSystem _zombify = default!;

    private Dictionary<string, string> _initialInfectedNames = new();

    public override string Prototype => "Zombie";

    private const string PatientZeroPrototypeID = "InitialInfected";
    private const string InitialZombieVirusPrototype = "PassiveZombieVirus";
    private const string ZombifySelfActionPrototype = "TurnUndead";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnJobAssigned);

        SubscribeLocalEvent<EntityZombifiedEvent>(OnEntityZombified);
        SubscribeLocalEvent<ZombifyOnDeathComponent, ZombifySelfActionEvent>(OnZombifySelf);
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        if (!RuleAdded)
            return;

        //this is just the general condition thing used for determining the win/lose text
        var percent = GetInfectedPercentage(out var livingHumans);

        if (percent <= 0)
            ev.AddLine(Loc.GetString("zombie-round-end-amount-none"));
        else if (percent <= 0.25)
            ev.AddLine(Loc.GetString("zombie-round-end-amount-low"));
        else if (percent <= 0.5)
            ev.AddLine(Loc.GetString("zombie-round-end-amount-medium", ("percent", Math.Round((percent * 100), 2).ToString())));
        else if (percent < 1)
            ev.AddLine(Loc.GetString("zombie-round-end-amount-high", ("percent", Math.Round((percent * 100), 2).ToString())));
        else
            ev.AddLine(Loc.GetString("zombie-round-end-amount-all"));

        ev.AddLine(Loc.GetString("zombie-round-end-initial-count", ("initialCount", _initialInfectedNames.Count)));
        foreach (var player in _initialInfectedNames)
        {
            ev.AddLine(Loc.GetString("zombie-round-end-user-was-initial",
                ("name", player.Key),
                ("username", player.Value)));
        }

        //Gets a bunch of the living players and displays them if they're under a threshold.
        //InitialInfected is used for the threshold because it scales with the player count well.
        if (livingHumans.Count > 0 && livingHumans.Count <= _initialInfectedNames.Count)
        {
            ev.AddLine("");
            ev.AddLine(Loc.GetString("zombie-round-end-survivor-count", ("count", livingHumans.Count)));
            foreach (var survivor in livingHumans)
            {
                var meta = MetaData(survivor);
                var username = string.Empty;
                if (TryComp<MindComponent>(survivor, out var mindcomp))
                    if (mindcomp.Mind != null && mindcomp.Mind.Session != null)
                        username = mindcomp.Mind.Session.Name;

                ev.AddLine(Loc.GetString("zombie-round-end-user-was-survivor",
                    ("name", meta.EntityName),
                    ("username", username)));
            }
        }
    }

    private void OnJobAssigned(RulePlayerJobsAssignedEvent ev)
    {
        if (!RuleAdded)
            return;

        _initialInfectedNames = new();

        InfectInitialPlayers();
    }

    /// <remarks>
    ///     This is just checked if the last human somehow dies
    ///     by starving or flying off into space.
    /// </remarks>
    private void OnMobStateChanged(MobStateChangedEvent ev)
    {
        if (!RuleAdded)
            return;
        CheckRoundEnd(ev.Entity);
    }

    private void OnEntityZombified(EntityZombifiedEvent ev)
    {
        if (!RuleAdded)
            return;
        CheckRoundEnd(ev.Target);
    }

    /// <summary>
    ///     The big kahoona function for checking if the round is gonna end
    /// </summary>
    /// <param name="target">depending on this uid, we should care about the round ending</param>
    private void CheckRoundEnd(EntityUid target)
    {
        //we only care about players, not monkeys and such.
        if (!HasComp<HumanoidComponent>(target))
            return;

        var percent = GetInfectedPercentage(out var num);
        if (num.Count == 1) //only one human left. spooky
           _popup.PopupEntity(Loc.GetString("zombie-alone"), num[0], num[0]);
        if (percent >= 1) //oops, all zombies
            _roundEndSystem.EndRound();
    }

    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        if (!RuleAdded)
            return;

        var minPlayers = _cfg.GetCVar(CCVars.ZombieMinPlayers);
        if (!ev.Forced && ev.Players.Length < minPlayers)
        {
            _chatManager.DispatchServerAnnouncement(Loc.GetString("zombie-not-enough-ready-players", ("readyPlayersCount", ev.Players.Length), ("minimumPlayers", minPlayers)));
            ev.Cancel();
            return;
        }

        if (ev.Players.Length == 0)
        {
            _chatManager.DispatchServerAnnouncement(Loc.GetString("zombie-no-one-ready"));
            ev.Cancel();
            return;
        }
    }

    public override void Started()
    {
        //this technically will run twice with zombies on roundstart, but it doesn't matter because it fails instantly
        InfectInitialPlayers();
    }

    public override void Ended() { }

    private void OnZombifySelf(EntityUid uid, ZombifyOnDeathComponent component, ZombifySelfActionEvent args)
    {
        _zombify.ZombifyEntity(uid);

        var action = new InstantAction(_prototypeManager.Index<InstantActionPrototype>(ZombifySelfActionPrototype));
        _action.RemoveAction(uid, action);
    }

    private float GetInfectedPercentage(out List<EntityUid> livingHumans)
    {
        var allPlayers = EntityQuery<HumanoidComponent, MobStateComponent>(true);
        var allZombers = GetEntityQuery<ZombieComponent>();

        var totalPlayers = new List<EntityUid>();
        var livingZombies = new List<EntityUid>();

        livingHumans = new();

        foreach (var (_, mob) in allPlayers)
        {
            if (_mobState.IsAlive(mob.Owner, mob))
            {
                totalPlayers.Add(mob.Owner);

                if (allZombers.HasComponent(mob.Owner))
                    livingZombies.Add(mob.Owner);
                else
                    livingHumans.Add(mob.Owner);
            }
        }
        return ((float) livingZombies.Count) / (float) totalPlayers.Count;
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
    private void InfectInitialPlayers()
    {
        var allPlayers = _playerManager.ServerSessions.ToList();
        var playerList = new List<IPlayerSession>();
        var prefList = new List<IPlayerSession>();
        foreach (var player in allPlayers)
        {
            if (player.AttachedEntity != null && HasComp<DiseaseCarrierComponent>(player.AttachedEntity))
            {
                playerList.Add(player);

                var pref = (HumanoidCharacterProfile) _prefs.GetPreferences(player.UserId).SelectedCharacter;
                if (pref.AntagPreferences.Contains(PatientZeroPrototypeID))
                    prefList.Add(player);
            }
        }

        if (playerList.Count == 0)
            return;

        var playersPerInfected = _cfg.GetCVar(CCVars.ZombiePlayersPerInfected);
        var maxInfected = _cfg.GetCVar(CCVars.ZombieMaxInitialInfected);

        var numInfected = Math.Max(1,
            (int) Math.Min(
                Math.Floor((double) playerList.Count / playersPerInfected), maxInfected));

        for (var i = 0; i < numInfected; i++)
        {
            IPlayerSession zombie;
            if (prefList.Count == 0)
            {
                if (playerList.Count == 0)
                {
                    Logger.InfoS("preset", "Insufficient number of players. stopping selection.");
                    break;
                }
                zombie = _random.PickAndTake(playerList);
                Logger.InfoS("preset", "Insufficient preferred patient 0, picking at random.");
            }
            else
            {
                zombie = _random.PickAndTake(prefList);
                playerList.Remove(zombie);
                Logger.InfoS("preset", "Selected a patient 0.");
            }

            var mind = zombie.Data.ContentData()?.Mind;
            if (mind == null)
            {
                Logger.ErrorS("preset", "Failed getting mind for picked patient 0.");
                continue;
            }

            DebugTools.AssertNotNull(mind.OwnedEntity);

            mind.AddRole(new TraitorRole(mind, _prototypeManager.Index<AntagPrototype>(PatientZeroPrototypeID)));

            var inCharacterName = string.Empty;
            if (mind.OwnedEntity != null)
            {
                _diseaseSystem.TryAddDisease(mind.OwnedEntity.Value, InitialZombieVirusPrototype);
                inCharacterName = MetaData(mind.OwnedEntity.Value).EntityName;

                var action = new InstantAction(_prototypeManager.Index<InstantActionPrototype>(ZombifySelfActionPrototype));
                _action.AddAction(mind.OwnedEntity.Value, action, null);
            }

            if (mind.Session != null)
            {
                var message = Loc.GetString("zombie-patientzero-role-greeting");
                var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));

                //gets the names now in case the players leave.
                //this gets unhappy if people with the same name get chose. Probably shouldn't happen.
                _initialInfectedNames.Add(inCharacterName, mind.Session.Name);

                // I went all the way to ChatManager.cs and all i got was this lousy T-shirt
                // You got a free T-shirt!?!?
                _chatManager.ChatMessageToOne(Shared.Chat.ChatChannel.Server, message,
                   wrappedMessage, default, false, mind.Session.ConnectedClient, Color.Plum);
            }
        }
    }
}
