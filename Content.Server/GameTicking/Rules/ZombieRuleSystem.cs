using System.Globalization;
using System.Linq;
using Content.Server.Actions;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind.Components;
using Content.Server.Players;
using Content.Server.Popups;
using Content.Server.Preferences.Managers;
using Content.Server.RoundEnd;
using Content.Server.Traitor;
using Content.Server.Zombies;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.CCVar;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Zombies;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking.Rules;

public sealed class ZombieRuleSystem : GameRuleSystem<ZombieRuleComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly ZombifyOnDeathSystem _zombify = default!;

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
        foreach (var zombie in EntityQuery<ZombieRuleComponent>())
        {
            // This is just the general condition thing used for determining the win/lose text
            var fraction = GetInfectedFraction();

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
            if (healthy.Count > 0 && healthy.Count <= 2 * zombie.InitialInfectedNames.Count)
            {
                ev.AddLine("");
                ev.AddLine(Loc.GetString("zombie-round-end-survivor-count", ("count", healthy.Count)));
                foreach (var survivor in healthy)
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
    }

    private void OnJobAssigned(RulePlayerJobsAssignedEvent ev)
    {
        var query = EntityQueryEnumerator<ZombieRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var zombies, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;
            InfectInitialPlayers(zombies);
        }
    }

    /// <remarks>
    ///     This is just checked if the last human somehow dies
    ///     by starving or flying off into space.
    /// </remarks>
    private void OnMobStateChanged(MobStateChangedEvent ev)
    {
        CheckRoundEnd(ev.Target);
    }

    private void OnEntityZombified(EntityZombifiedEvent ev)
    {
        CheckRoundEnd(ev.Target);
    }

    /// <summary>
    ///     The big kahoona function for checking if the round is gonna end
    /// </summary>
    /// <param name="target">depending on this uid, we should care about the round ending</param>
    private void CheckRoundEnd(EntityUid target)
    {
        var query = EntityQueryEnumerator<ZombieRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var zombies, out var gameRule))
        {
            if (GameTicker.IsGameRuleActive(uid, gameRule))
                continue;

            // We only care about players, not monkeys and such.
            if (!HasComp<HumanoidAppearanceComponent>(target))
                continue;

            var fraction = GetInfectedFraction();
            var healthy = GetHealthyHumans();
            if (healthy.Count == 1) // Only one human left. spooky
                _popup.PopupEntity(Loc.GetString("zombie-alone"), healthy[0], healthy[0]);
            if (fraction >= 1) // Oops, all zombies
                _roundEndSystem.EndRound();
        }
    }

    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        var query = EntityQueryEnumerator<ZombieRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var zombies, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            var minPlayers = _cfg.GetCVar(CCVars.ZombieMinPlayers);
            if (!ev.Forced && ev.Players.Length < minPlayers)
            {
                _chatManager.DispatchServerAnnouncement(Loc.GetString("zombie-not-enough-ready-players", ("readyPlayersCount", ev.Players.Length), ("minimumPlayers", minPlayers)));
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
        InfectInitialPlayers(component);
    }

    private void OnZombifySelf(EntityUid uid, ZombifyOnDeathComponent component, ZombifySelfActionEvent args)
    {
        _zombify.ZombifyEntity(uid);

        var action = new InstantAction(_prototypeManager.Index<InstantActionPrototype>(ZombieRuleComponent.ZombifySelfActionPrototype));
        _action.RemoveAction(uid, action);
    }

    private float GetInfectedFraction()
    {
        var players = EntityQuery<HumanoidAppearanceComponent>(true);
        var zombers = EntityQuery<HumanoidAppearanceComponent, ZombieComponent>(true);

        return zombers.Count() / (float) players.Count();
    }

    private List<EntityUid> GetHealthyHumans()
    {
        var healthy = new List<EntityUid>();
        var players = AllEntityQuery<HumanoidAppearanceComponent, MobStateComponent>();
        var zombers = GetEntityQuery<ZombieComponent>();
        while (players.MoveNext(out var uid, out _, out var mob))
        {
            if (_mobState.IsAlive(uid, mob) && !zombers.HasComponent(uid))
            {
                healthy.Add(uid);
            }
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
        var allPlayers = _playerManager.ServerSessions.ToList();
        var playerList = new List<IPlayerSession>();
        var prefList = new List<IPlayerSession>();
        foreach (var player in allPlayers)
        {
            // TODO: A
            if (player.AttachedEntity != null && HasComp<HumanoidAppearanceComponent>(player.AttachedEntity))
            {
                playerList.Add(player);

                var pref = (HumanoidCharacterProfile) _prefs.GetPreferences(player.UserId).SelectedCharacter;
                if (pref.AntagPreferences.Contains(component.PatientZeroPrototypeID))
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

            mind.AddRole(new TraitorRole(mind, _prototypeManager.Index<AntagPrototype>(component.PatientZeroPrototypeID)));

            var inCharacterName = string.Empty;
            if (mind.OwnedEntity != null)
            {
                EnsureComp<PendingZombieComponent>(mind.OwnedEntity.Value);
                EnsureComp<ZombifyOnDeathComponent>(mind.OwnedEntity.Value);
                inCharacterName = MetaData(mind.OwnedEntity.Value).EntityName;

                var action = new InstantAction(_prototypeManager.Index<InstantActionPrototype>(ZombieRuleComponent.ZombifySelfActionPrototype));
                _action.AddAction(mind.OwnedEntity.Value, action, null);
            }

            if (mind.Session != null)
            {
                var message = Loc.GetString("zombie-patientzero-role-greeting");
                var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));

                //gets the names now in case the players leave.
                //this gets unhappy if people with the same name get chose. Probably shouldn't happen.
                component.InitialInfectedNames.Add(inCharacterName, mind.Session.Name);

                // I went all the way to ChatManager.cs and all i got was this lousy T-shirt
                // You got a free T-shirt!?!?
                _chatManager.ChatMessageToOne(Shared.Chat.ChatChannel.Server, message,
                   wrappedMessage, default, false, mind.Session.ConnectedClient, Color.Plum);
            }
        }
    }
}
