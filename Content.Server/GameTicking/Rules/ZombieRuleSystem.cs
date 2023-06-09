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
using Robust.Shared.Timing;
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
    [Dependency] private readonly ZombieSystem _zombie = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);

        SubscribeLocalEvent<EntityZombifiedEvent>(OnEntityZombified);
        SubscribeLocalEvent<InitialInfectedComponent, ZombifySelfActionEvent>(OnZombifySelf);
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        var healthy = GetHealthyHumans();

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

        int infectedNames = 0;
        foreach (var zombie in EntityQuery<ZombieRuleComponent>())
        {
            ev.AddLine(Loc.GetString("zombie-round-end-initial-count",
                ("initialCount", zombie.InitialInfectedNames.Count)));
            foreach (var player in zombie.InitialInfectedNames)
            {
                ev.AddLine(Loc.GetString("zombie-round-end-user-was-initial",
                    ("name", player.Key),
                    ("username", player.Value)));
            }

            infectedNames += zombie.InitialInfectedNames.Count;
        }

        // Gets a bunch of the living players and displays them if they're under a threshold.
        // InitialInfected is used for the threshold because it scales with the player count well.
        if (healthy.Count > 0 && healthy.Count <= 2 * infectedNames)
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
        // We only care about players, not monkeys and such.
        if (!HasComp<HumanoidAppearanceComponent>(target))
            return;

        var query = EntityQueryEnumerator<ZombieRuleComponent, GameRuleComponent>();

        var fraction = 0.0f;
        var healthyCount = -1;
        while (query.MoveNext(out var uid, out var zombies, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                continue;

            if (healthyCount == -1)
            {
                // Code run in the first relevant zombie rule, though there might be many of them.

                fraction = GetInfectedFraction();
                healthyCount = CountHealthyHumans();

                if (healthyCount == 1)
                {
                    // Only one human left. spooky
                    var healthy = GetHealthyHumans();
                    _popup.PopupEntity(Loc.GetString("zombie-alone"), healthy[0], healthy[0]);
                }

                if (fraction >= 1) // Oops, all zombies
                    _roundEndSystem.EndRound();

                if (zombies.ShuttleCalls.Count > 0 && fraction >= zombies.ShuttleCalls[0])
                {
                    // Call shuttle if not called
                    zombies.ShuttleCalls.RemoveAt(0);
                    _roundEndSystem.RequestRoundEnd(uid);
                }
            }

            if (!zombies.ForcedZombies && fraction > zombies.ForceZombiesAt)
            {
                zombies.ForcedZombies = true;
                _zombie.ForceZombies(uid, zombies);
            }

            CheckRuleEnd(uid, zombies, gameRule);
        }
    }

    public void CheckRuleEnd(EntityUid ruleUid, ZombieRuleComponent? zombies = null, GameRuleComponent? gameRule = null)
    {
        if (!Resolve(ruleUid, ref zombies, ref gameRule))
            return;

        // Check that we've picked our zombies
        if (zombies.InfectInitialAt != TimeSpan.Zero)
            return;

        // Look for living zombies
        var livingQuery = GetEntityQuery<LivingZombieComponent>();
        var initialQuery = GetEntityQuery<InitialInfectedComponent>();
        int livingZombies = 0;
        int deadZombies = 0;
        int initialZombies = 0;
        var zombers = EntityQueryEnumerator<ZombieComponent>();
        while (zombers.MoveNext(out var uid, out var zombie))
        {
            if (zombie.Family.Rules != ruleUid)
                continue;

            if (livingQuery.HasComponent(uid))
            {
                livingZombies += 1;
            }
            else if (initialQuery.HasComponent(uid))
            {
                initialZombies += 1;
            }
            else
            {
                deadZombies += 1;
            }
        }

        if (initialZombies == 0 && livingZombies == 0)
        {
            GameTicker.EndGameRule(ruleUid, gameRule);
            if (zombies.WinEndsRoundAbove < 1.0f)
            {
                if (zombies.WinEndsRoundAbove <= 0.0f)
                {
                    // Human victory (skip the check)
                    _roundEndSystem.EndRound();
                }
                else
                {
                    // This fraction is only counting zombies from the current outbreak. See how much they outnumber
                    // the living.
                    var healthyCount = CountHealthyHumans();
                    var fraction = (float)deadZombies / (float)(deadZombies + healthyCount);
                    if (fraction > zombies.WinEndsRoundAbove)
                    {
                        // Human victory
                        _roundEndSystem.EndRound();
                    }
                }
            }
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
        var curTime = _timing.CurTime;

        component.AnnounceAt = curTime +
                               TimeSpan.FromSeconds(_random.NextFloat(component.AnnounceMin,
                                   component.AnnounceMax));
        component.InfectInitialAt = curTime + TimeSpan.FromSeconds(component.InitialInfectDelaySecs);

        component.FirstTurnAllowed = curTime + TimeSpan.FromSeconds(component.TurnTimeMin);

        if (component.EarlySettings.EmoteSoundsId != null)
        {
            _prototypeManager.TryIndex(component.EarlySettings.EmoteSoundsId, out component.EarlySettings.EmoteSounds);
        }
        if (component.VictimSettings.EmoteSoundsId != null)
        {
            _prototypeManager.TryIndex(component.VictimSettings.EmoteSoundsId, out component.VictimSettings.EmoteSounds);
        }
    }

    protected override void ActiveTick(EntityUid uid, ZombieRuleComponent component, GameRuleComponent gameRule,
        float frameTime)
    {
        var curTime = _timing.CurTime;
        if (component.InfectInitialAt != TimeSpan.Zero && component.InfectInitialAt < curTime)
        {
            // Time to infect the initial players
            InfectInitialPlayers(uid, component);
            component.InfectInitialAt = TimeSpan.Zero;
        }

        if (component.AnnounceAt != TimeSpan.Zero && component.AnnounceAt < curTime)
        {
            // Announce the brain eating about to commence
            if (_random.Prob(component.AnnounceChance))
            {
                _chatManager.DispatchServerAnnouncement(Loc.GetString("zombies-will-eat-your-brains"));
            }
            component.AnnounceAt = TimeSpan.Zero;
        }

        if (component.FirstTurnAllowed != TimeSpan.Zero && component.FirstTurnAllowed < curTime)
        {
            ActivateZombifyOnDeath(uid, component);
            component.FirstTurnAllowed = TimeSpan.Zero;
        }

        var query = EntityQueryEnumerator<InitialInfectedComponent, ZombieComponent>();
        while (query.MoveNext(out var initialUid, out var initial, out var zombie))
        {
            if (initial.TurnForced < curTime)
            {
                // Time to begin forcing this initial infected to turn.
                var pending = EnsureComp<PendingZombieComponent>(initialUid);
                pending.MaxInfectionLength = zombie.Settings.InfectionTurnTime;
                pending.InfectionStarted = _timing.CurTime;
                pending.VirusDamage = zombie.Settings.VirusDamage;

                RemCompDeferred<InitialInfectedComponent>(initialUid);
            }
        }
    }

    private void OnZombifySelf(EntityUid uid, InitialInfectedComponent initial, ZombifySelfActionEvent args)
    {
        // Check it's not too early to zombify
        if (initial.FirstTurnAllowed != TimeSpan.Zero)
            return;

        _zombie.ZombifyEntity(uid);

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
        var zombers = GetEntityQuery<LivingZombieComponent>();
        while (players.MoveNext(out var uid, out _, out var mob))
        {
            if (_mobState.IsAlive(uid, mob) && !zombers.HasComponent(uid))
            {
                healthy.Add(uid);
            }
        }
        return healthy;
    }

    private int CountHealthyHumans()
    {
        var healthy = 0;
        var players = AllEntityQuery<HumanoidAppearanceComponent, MobStateComponent>();
        var zombers = GetEntityQuery<LivingZombieComponent>();
        while (players.MoveNext(out var uid, out _, out var mob))
        {
            if (_mobState.IsAlive(uid, mob) && !zombers.HasComponent(uid))
            {
                healthy += 1;
            }
        }
        return healthy;
    }

    public void AddToInfectedList(EntityUid uid, ZombieComponent zombie, ZombieRuleComponent rules, MindComponent? mindComponent = null)
    {
        if (!Resolve(uid, ref mindComponent))
            return;

        var mind = mindComponent.Mind;
        if (mind?.Session != null && mind.OwnedEntity != null)
        {
            var inCharacterName = MetaData(mind.OwnedEntity.Value).EntityName;
            rules.InitialInfectedNames.Add(inCharacterName, mind.Session.Name);
        }
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
    private void InfectInitialPlayers(EntityUid uid, ZombieRuleComponent rules)
    {
        var allPlayers = _playerManager.ServerSessions.ToList();
        var playerList = new List<IPlayerSession>();
        var prefList = new List<IPlayerSession>();

        foreach (var player in allPlayers)
        {

            if (player.AttachedEntity != null && HasComp<HumanoidAppearanceComponent>(player.AttachedEntity))
            {
                if (TryComp<ZombieComponent>(player.AttachedEntity, out var zombie))
                {
                    // This player is already a zombie. If they don't already have a rule, add them to this one.
                    if (zombie.Family.Rules == EntityUid.Invalid)
                    {
                        zombie.Family.Rules = uid;
                        zombie.Settings = rules.EarlySettings;
                        zombie.VictimSettings = rules.VictimSettings;

                        var mind = player.Data.ContentData()?.Mind;
                        if (mind?.Session != null)
                        {
                            rules.InitialInfectedNames[zombie.BeforeZombifiedEntityName] = mind.Session.Name;
                        }
                    }
                }
                else
                {
                    playerList.Add(player);

                    var pref = (HumanoidCharacterProfile) _prefs.GetPreferences(player.UserId).SelectedCharacter;
                    if (pref.AntagPreferences.Contains(rules.PatientZeroPrototypeID))
                        prefList.Add(player);
                }
            }
        }

        // Check for mindless zombies (not attached to players) that still don't have a rules entity attached...
        var zombers = EntityQueryEnumerator<ZombieComponent, LivingZombieComponent>();
        while (zombers.MoveNext(out var zombUid, out var zombie, out var living))
        {
            if (zombie.Family.Rules == EntityUid.Invalid)
            {
                // Note that we add this zombie to the new rules, but we don't count them towards the num infected. They
                // are not inhabited by a player. Were we to count them, we should probably also check they are still
                // alive.
                zombie.Family.Rules = uid;
                zombie.Settings = rules.EarlySettings;
                zombie.VictimSettings = rules.VictimSettings;
            }
        }

        if (playerList.Count == 0)
            return;

        var playersPerInfected = Math.Max(rules.PlayersPerInfected, _cfg.GetCVar(CCVars.ZombiePlayersPerInfected));
        var maxInfected = Math.Min(rules.MaxInitialInfected, _cfg.GetCVar(CCVars.ZombieMaxInitialInfected));

        var numInfected = (int)Math.Clamp(Math.Floor((double) playerList.Count / playersPerInfected), 1, maxInfected);
        var curTime = _timing.CurTime;

        // These are already infected (by admins probably)
        numInfected -= rules.InitialInfectedNames.Count;

        // How long the zombies have as a group to decide to begin their attack.
        //   Varies randomly from 10 to 15 minutes. After this the virus begins and they start
        //   taking zombie virus damage.
        var groupTimelimit = _random.NextFloat(rules.MinZombieForceSecs, rules.MaxZombieForceSecs);
        for (var i = 0; i < numInfected; i++)
        {
            IPlayerSession zombiePlayer;
            if (prefList.Count == 0)
            {
                if (playerList.Count == 0)
                {
                    Logger.InfoS("preset", "Insufficient number of players. stopping selection.");
                    break;
                }
                zombiePlayer = _random.PickAndTake(playerList);
                Logger.InfoS("preset", "Insufficient preferred patient 0, picking at random.");
            }
            else
            {
                zombiePlayer = _random.PickAndTake(prefList);
                playerList.Remove(zombiePlayer);
                Logger.InfoS("preset", "Selected a patient 0.");
            }

            var mind = zombiePlayer.Data.ContentData()?.Mind;
            if (mind == null)
            {
                Logger.ErrorS("preset", "Failed getting mind for picked patient 0.");
                continue;
            }

            DebugTools.AssertNotNull(mind.OwnedEntity);

            mind.AddRole(new TraitorRole(mind, _prototypeManager.Index<AntagPrototype>(rules.PatientZeroPrototypeID)));

            var inCharacterName = string.Empty;
            // Create some variation between the times of each zombie, relative to the time of the group as a whole.
            var personalDelay = _random.NextFloat(0.0f, rules.PlayerZombieForceVariationSecs);
            if (mind.OwnedEntity != null)
            {
                var initial = EnsureComp<InitialInfectedComponent>(mind.OwnedEntity.Value);
                initial.FirstTurnAllowed = rules.FirstTurnAllowed;
                // Only take damage after this many seconds
                initial.TurnForced = curTime + TimeSpan.FromSeconds(groupTimelimit + personalDelay);

                var zombie = EnsureComp<ZombieComponent>(mind.OwnedEntity.Value);
                // Patient zero zombies get one set of zombie settings, later zombies get a different (less powerful) set.
                zombie.Settings = rules.EarlySettings;
                zombie.VictimSettings = rules.VictimSettings;
                zombie.Family = new ZombieFamily() { Rules = uid, Generation = 0 };

                inCharacterName = MetaData(mind.OwnedEntity.Value).EntityName;

                var action = new InstantAction(_prototypeManager.Index<InstantActionPrototype>(ZombieRuleComponent.ZombifySelfActionPrototype));

                // Set a cooldown on the action here that reflects the time until initial infection.
                action.Cooldown = (curTime, rules.FirstTurnAllowed);
                _action.AddAction(mind.OwnedEntity.Value, action, null);
            }

            if (mind.Session != null)
            {
                var message = Loc.GetString("zombie-patientzero-role-greeting");
                var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));

                //gets the names now in case the players leave.
                //this gets unhappy if people with the same name get chose. Probably shouldn't happen.
                rules.InitialInfectedNames[inCharacterName] = mind.Session.Name;

                // I went all the way to ChatManager.cs and all i got was this lousy T-shirt
                // You got a free T-shirt!?!?
                _chatManager.ChatMessageToOne(Shared.Chat.ChatChannel.Server, message,
                   wrappedMessage, default, false, mind.Session.ConnectedClient, Color.Plum);
            }
        }
    }

    private void ActivateZombifyOnDeath(EntityUid ruleUid, ZombieRuleComponent component)
    {
        var query = EntityQueryEnumerator<InitialInfectedComponent, ZombieComponent, MobStateComponent>();
        while (query.MoveNext(out var uid, out var initial, out var zombie, out var mobState))
        {
            // Don't change zombies that don't belong to these rules.
            if (zombie.Family.Rules != ruleUid)
                continue;

            // This is probably already in the past, but just in case the rule has had a time shortened, set it now.
            initial.FirstTurnAllowed = TimeSpan.Zero;

            if (mobState.CurrentState == MobState.Dead)
            {
                // Zombify them immediately
                _zombie.ZombifyEntity(uid, mobState);
            }
            else if (mobState.CurrentState == MobState.Critical)
            {
                // Immediately jump to an active virus now
                var pending = EnsureComp<PendingZombieComponent>(uid);
                pending.MaxInfectionLength = zombie.Settings.InfectionTurnTime;
                pending.InfectionStarted = _timing.CurTime;
                pending.VirusDamage = zombie.Settings.VirusDamage;

                RemCompDeferred<InitialInfectedComponent>(uid);
            }
        }
    }

    public (EntityUid, ZombieRuleComponent?) FindActiveRule()
    {
        var query = EntityQueryEnumerator<ZombieRuleComponent, GameRuleComponent>();

        while (query.MoveNext(out var uid, out var zombies, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                continue;

            return (uid, zombies);
        }

        return (EntityUid.Invalid, null);
    }
}
