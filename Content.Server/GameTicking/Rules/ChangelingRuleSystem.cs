using System.Linq;
using Content.Server.Antag;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.NPC.Systems;
using Content.Server.Objectives;
using Content.Shared.IdentityManagement;
using Content.Server.Roles;
using Content.Server.Shuttles.Components;
using Content.Shared.CCVar;
using Content.Shared.Dataset;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Objectives.Components;
using Content.Shared.Changeling.Components;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Server.Actions;

namespace Content.Server.GameTicking.Rules;

public sealed class ChangelingRuleSystem : GameRuleSystem<ChangelingRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antagSelection = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
    [Dependency] private readonly SharedJobSystem _jobs = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;

    private int PlayersPerLing => _cfg.GetCVar(CCVars.ChangelingPlayersPerChangeling);
    private int MaxChangelings => _cfg.GetCVar(CCVars.ChangelingMaxChangelings);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayersSpawned);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(HandleLatejoin);

        SubscribeLocalEvent<ChangelingRuleComponent, ObjectivesTextGetInfoEvent>(OnObjectivesTextGetInfo);
    }

    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        var query = EntityQueryEnumerator<ChangelingRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var changeling, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            var minPlayers = _cfg.GetCVar(CCVars.ChangelingMinPlayers);
            if (!ev.Forced && ev.Players.Length < minPlayers)
            {
                _chatManager.SendAdminAnnouncement(Loc.GetString("changelings-not-enough-ready-players",
                    ("readyPlayersCount", ev.Players.Length), ("minimumPlayers", minPlayers)));
                ev.Cancel();
                continue;
            }

            if (ev.Players.Length == 0)
            {
                _chatManager.DispatchServerAnnouncement(Loc.GetString("changelings-no-one-ready"));
                ev.Cancel();
            }
        }
    }

    private void DoChangelingStart(ChangelingRuleComponent component)
    {
        if (!component.StartCandidates.Any())
        {
            Log.Error("Tried to start Changeling mode without any candidates.");
            return;
        }

        var numLings = MathHelper.Clamp(component.StartCandidates.Count / PlayersPerLing, 1, MaxChangelings);
        var lingPool = _antagSelection.FindPotentialAntags(component.StartCandidates, component.ChangelingPrototypeId);
        var selectedLings = _antagSelection.PickAntag(numLings, lingPool);

        foreach (var ling in selectedLings)
        {
            MakeChangeling(ling);
        }
    }

    private void OnPlayersSpawned(RulePlayerJobsAssignedEvent ev)
    {
        var query = EntityQueryEnumerator<ChangelingRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var ling, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;
            foreach (var player in ev.Players)
            {
                if (!ev.Profiles.ContainsKey(player.UserId))
                    continue;

                ling.StartCandidates[player] = ev.Profiles[player.UserId];
            }

            DoChangelingStart(ling);
        }
    }

    public bool MakeChangeling(ICommonSession changeling)
    {
        var lingRule = EntityQuery<ChangelingRuleComponent>().FirstOrDefault();
        if (lingRule == null)
        {
            GameTicker.StartGameRule("Changeling", out var ruleEntity);
            lingRule = Comp<ChangelingRuleComponent>(ruleEntity);
        }

        if (!_mindSystem.TryGetMind(changeling, out var mindId, out var mind))
        {
            Log.Info("Failed getting mind for picked changeling.");
            return false;
        }

        if (HasComp<ChangelingRoleComponent>(mindId))
        {
            Log.Error($"Player {changeling.Name} is already a changeling.");
            return false;
        }

        if (mind.OwnedEntity is not { } entity)
        {
            Log.Error("Mind picked for traitor did not have an attached entity.");
            return false;
        }

        var briefing = Loc.GetString("changeling-role-greeting-short", ("character-name", Identity.Entity(entity, EntityManager)));

        // Prepare changeling role
        var lingRole = new ChangelingRoleComponent
        {
            PrototypeId = lingRule.ChangelingPrototypeId,
        };

        // Assign changeling role
        _roleSystem.MindAddRole(mindId, new ChangelingRoleComponent
        {
            PrototypeId = lingRule.ChangelingPrototypeId
        }, mind);
        // Assign briefing and greeting sound
        _roleSystem.MindAddRole(mindId, new RoleBriefingComponent
        {
            Briefing = briefing,
        }, mind);
        _roleSystem.MindPlaySound(mindId, lingRule.ChangelingStartSound, mind);
        SendChangelingBriefing(mindId, entity);
        lingRule.ChangelingMinds.Add(mindId);

        // Change the faction
        _npcFaction.RemoveFaction(entity, "NanoTrasen", false);
        _npcFaction.AddFaction(entity, "Syndicate");

        EnsureComp<ChangelingComponent>(entity);

        // Give lings their objectives
        var maxDifficulty = 5;
        var maxPicks = _cfg.GetCVar(CCVars.ChangelingMaxPicks);
        var difficulty = 0f;
        for (var pick = 0; pick < maxPicks && maxDifficulty > difficulty; pick++)
        {
            var objective = _objectives.GetRandomObjective(mindId, mind, "ChangelingObjectiveGroups");
            if (objective == null)
                continue;

            _mindSystem.AddObjective(mindId, mind, objective.Value);
            difficulty += Comp<ObjectiveComponent>(objective.Value).Difficulty;
        }

        return true;
    }

    /// <summary>
    ///     Send a codewords and uplink codes to traitor chat.
    /// </summary>
    /// <param name="mind">A mind (player)</param>
    private void SendChangelingBriefing(EntityUid mind, EntityUid ownedEntity)
    {
        if (!_mindSystem.TryGetSession(mind, out var session))
            return;

        var message = Loc.GetString("changeling-role-greeting", ("character-name", Identity.Entity(ownedEntity, EntityManager)));
        var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
        _chatManager.ChatMessageToOne(Shared.Chat.ChatChannel.Server, message, wrappedMessage, default, false, session.ConnectedClient, Color.FromHex("#FF0000"));
    }

    private void HandleLatejoin(PlayerSpawnCompleteEvent ev)
    {
        var query = EntityQueryEnumerator<ChangelingRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var ling, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            if (ling.TotalChangelings >= MaxChangelings)
                continue;
            if (!ev.LateJoin)
                continue;
            if (!ev.Profile.AntagPreferences.Contains(ling.ChangelingPrototypeId))
                continue;

            if (ev.JobId == null || !_prototypeManager.TryIndex<JobPrototype>(ev.JobId, out var job))
                continue;
            if (!job.CanBeAntag)
                continue;

            // the nth player we adjust our probabilities around
            var target = PlayersPerLing * ling.TotalChangelings + 1;

            var chance = 1f / PlayersPerLing;

            // If we have too many traitors, divide by how many players below target for next traitor we are.
            if (ev.JoinOrder < target)
            {
                chance /= (target - ev.JoinOrder);
            }
            else // Tick up towards 100% chance.
            {
                chance *= ((ev.JoinOrder + 1) - target);
            }

            if (chance > 1)
                chance = 1;

            // Now that we've calculated our chance, roll and make them a traitor if we roll under.
            // You get one shot.
            if (_random.Prob(chance))
            {
                MakeChangeling(ev.Player);
            }
        }
    }

    private void OnObjectivesTextGetInfo(EntityUid uid, ChangelingRuleComponent comp, ref ObjectivesTextGetInfoEvent args)
    {
        args.Minds = comp.ChangelingMinds;
        args.AgentName = Loc.GetString("ling-round-end-name");
    }

    public List<(EntityUid Id, MindComponent Mind)> GetOtherChangelingMindsAliveAndConnected(MindComponent ourMind)
    {
        List<(EntityUid Id, MindComponent Mind)> allChangelings = new();
        foreach (var changeling in EntityQuery<ChangelingRuleComponent>())
        {
            foreach (var role in GetOtherChangelingMindsAliveAndConnected(ourMind, changeling))
            {
                if (!allChangelings.Contains(role))
                    allChangelings.Add(role);
            }
        }

        return allChangelings;
    }

    private List<(EntityUid Id, MindComponent Mind)> GetOtherChangelingMindsAliveAndConnected(MindComponent ourMind, ChangelingRuleComponent component)
    {
        var changelings = new List<(EntityUid Id, MindComponent Mind)>();
        foreach (var changeling in component.ChangelingMinds)
        {
            if (TryComp(changeling, out MindComponent? mind) &&
                mind.OwnedEntity != null &&
                mind.Session != null &&
                mind != ourMind &&
                _mobStateSystem.IsAlive(mind.OwnedEntity.Value) &&
                mind.CurrentEntity == mind.OwnedEntity)
            {
                changelings.Add((changeling, mind));
            }
        }

        return changelings;
    }
}