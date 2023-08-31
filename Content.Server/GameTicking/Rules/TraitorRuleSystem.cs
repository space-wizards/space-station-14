using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.NPC.Systems;
using Content.Server.Objectives;
using Content.Server.PDA.Ringer;
using Content.Server.Roles;
using Content.Server.Roles.Jobs;
using Content.Server.Shuttles.Components;
using Content.Server.Traitor.Uplink;
using Content.Shared.CCVar;
using Content.Shared.Dataset;
using Content.Shared.Mobs.Systems;
using Content.Shared.PDA;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.GameTicking.Rules;

public sealed class TraitorRuleSystem : GameRuleSystem<TraitorRuleComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IComponentFactory _component = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly UplinkSystem _uplink = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly RoleSystem _roleSystem = default!;
    [Dependency] private readonly JobSystem _jobs = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;

    private int PlayersPerTraitor => _cfg.GetCVar(CCVars.TraitorPlayersPerTraitor);
    private int MaxTraitors => _cfg.GetCVar(CCVars.TraitorMaxTraitors);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayersSpawned);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(HandleLatejoin);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
    }

    protected override void ActiveTick(EntityUid uid, TraitorRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        if (component.SelectionStatus == TraitorRuleComponent.SelectionState.ReadyToSelect && _gameTiming.CurTime > component.AnnounceAt)
            DoTraitorStart(component);
    }

    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        var query = EntityQueryEnumerator<TraitorRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var traitor, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            MakeCodewords(traitor);

            var minPlayers = _cfg.GetCVar(CCVars.TraitorMinPlayers);
            if (!ev.Forced && ev.Players.Length < minPlayers)
            {
                _chatManager.SendAdminAnnouncement(Loc.GetString("traitor-not-enough-ready-players",
                    ("readyPlayersCount", ev.Players.Length), ("minimumPlayers", minPlayers)));
                ev.Cancel();
                continue;
            }

            if (ev.Players.Length == 0)
            {
                _chatManager.DispatchServerAnnouncement(Loc.GetString("traitor-no-one-ready"));
                ev.Cancel();
            }
        }
    }

    private void MakeCodewords(TraitorRuleComponent component)
    {
        var codewordCount = _cfg.GetCVar(CCVars.TraitorCodewordCount);
        var adjectives = _prototypeManager.Index<DatasetPrototype>("adjectives").Values;
        var verbs = _prototypeManager.Index<DatasetPrototype>("verbs").Values;
        var codewordPool = adjectives.Concat(verbs).ToList();
        var finalCodewordCount = Math.Min(codewordCount, codewordPool.Count);
        component.Codewords = new string[finalCodewordCount];
        for (var i = 0; i < finalCodewordCount; i++)
        {
            component.Codewords[i] = _random.PickAndTake(codewordPool);
        }
    }

    private void DoTraitorStart(TraitorRuleComponent component)
    {
        if (!component.StartCandidates.Any())
        {
            Log.Error("Tried to start Traitor mode without any candidates.");
            return;
        }

        var numTraitors = MathHelper.Clamp(component.StartCandidates.Count / PlayersPerTraitor, 1, MaxTraitors);
        var traitorPool = FindPotentialTraitors(component.StartCandidates, component);
        var selectedTraitors = PickTraitors(numTraitors, traitorPool);

        foreach (var traitor in selectedTraitors)
        {
            MakeTraitor(traitor);
        }

        component.SelectionStatus = TraitorRuleComponent.SelectionState.SelectionMade;
    }

    private void OnPlayersSpawned(RulePlayerJobsAssignedEvent ev)
    {
        var query = EntityQueryEnumerator<TraitorRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var traitor, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;
            foreach (var player in ev.Players)
            {
                if (!ev.Profiles.ContainsKey(player.UserId))
                    continue;

                traitor.StartCandidates[player] = ev.Profiles[player.UserId];
            }

            var delay = TimeSpan.FromSeconds(
                _cfg.GetCVar(CCVars.TraitorStartDelay) +
                _random.NextFloat(0f, _cfg.GetCVar(CCVars.TraitorStartDelayVariance)));

            traitor.AnnounceAt = _gameTiming.CurTime + delay;

            traitor.SelectionStatus = TraitorRuleComponent.SelectionState.ReadyToSelect;
        }
    }

    private List<IPlayerSession> FindPotentialTraitors(in Dictionary<IPlayerSession, HumanoidCharacterProfile> candidates, TraitorRuleComponent component)
    {
        var list = new List<IPlayerSession>();
        var pendingQuery = GetEntityQuery<PendingClockInComponent>();

        foreach (var player in candidates.Keys)
        {
            // Role prevents antag.
            if (!_jobs.CanBeAntag(player))
            {
                continue;
            }

            // Latejoin
            if (player.AttachedEntity != null && pendingQuery.HasComponent(player.AttachedEntity.Value))
                continue;

            list.Add(player);
        }

        var prefList = new List<IPlayerSession>();

        foreach (var player in list)
        {
            var profile = candidates[player];
            if (profile.AntagPreferences.Contains(component.TraitorPrototypeId))
            {
                prefList.Add(player);
            }
        }
        if (prefList.Count == 0)
        {
            Log.Info("Insufficient preferred traitors, picking at random.");
            prefList = list;
        }
        return prefList;
    }

    private List<IPlayerSession> PickTraitors(int traitorCount, List<IPlayerSession> prefList)
    {
        var results = new List<IPlayerSession>(traitorCount);
        if (prefList.Count == 0)
        {
            Log.Info("Insufficient ready players to fill up with traitors, stopping the selection.");
            return results;
        }

        for (var i = 0; i < traitorCount; i++)
        {
            results.Add(_random.PickAndTake(prefList));
            Log.Info("Selected a preferred traitor.");
        }
        return results;
    }

    public bool MakeTraitor(IPlayerSession traitor)
    {
        var traitorRule = EntityQuery<TraitorRuleComponent>().FirstOrDefault();
        if (traitorRule == null)
        {
            //todo fuck me this shit is awful
            //no i wont fuck you, erp is against rules
            GameTicker.StartGameRule("Traitor", out var ruleEntity);
            traitorRule = Comp<TraitorRuleComponent>(ruleEntity);
            MakeCodewords(traitorRule);
        }

        if (!_mindSystem.TryGetMind(traitor, out var mindId, out var mind))
        {
            Log.Info("Failed getting mind for picked traitor.");
            return false;
        }

        if (HasComp<TraitorRoleComponent>(mindId))
        {
            Log.Error($"Player {traitor.Name} is already a traitor.");
            return false;
        }

        if (mind.OwnedEntity is not { } entity)
        {
            Log.Error("Mind picked for traitor did not have an attached entity.");
            return false;
        }

        // Calculate the amount of currency on the uplink.
        var startingBalance = _cfg.GetCVar(CCVars.TraitorStartingBalance);
        if (_jobs.MindTryGetJob(mindId, out _, out var prototype))
            startingBalance = Math.Max(startingBalance - prototype.AntagAdvantage, 0);

        // creadth: we need to create uplink for the antag.
        // PDA should be in place already
        var pda = _uplink.FindUplinkTarget(mind.OwnedEntity!.Value);
        if (pda == null || !_uplink.AddUplink(mind.OwnedEntity.Value, startingBalance))
            return false;

        // Add the ringtone uplink and get its code for greeting
        var code = EnsureComp<RingerUplinkComponent>(pda.Value).Code;

        // Prepare traitor role
        // Give traitors their codewords and uplink code to keep in their character info menu
        var briefing =
            $"{Loc.GetString("traitor-role-codewords-short", ("codewords", string.Join(", ", traitorRule.Codewords)))}\n{Loc.GetString("traitor-role-uplink-code-short", ("code", string.Join("-", code).Replace("sharp", "#")))}";
        var traitorRole = new TraitorRoleComponent
        {
            PrototypeId = traitorRule.TraitorPrototypeId,
            Briefing = briefing
        };

        // Assign traitor roles
        _roleSystem.MindAddRole(mindId, traitorRole);
        SendTraitorBriefing(mindId, traitorRule.Codewords, code);
        traitorRule.TraitorMinds.Add(mindId);

        if (_mindSystem.TryGetSession(mindId, out var session))
        {
            // Notificate player about new role assignment
            _audioSystem.PlayGlobal(traitorRule.GreetSoundNotification, session);
        }

        // Change the faction
        _npcFaction.RemoveFaction(entity, "NanoTrasen", false);
        _npcFaction.AddFaction(entity, "Syndicate");

        // Give traitors their objectives
        var maxDifficulty = _cfg.GetCVar(CCVars.TraitorMaxDifficulty);
        var maxPicks = _cfg.GetCVar(CCVars.TraitorMaxPicks);
        var difficulty = 0f;
        for (var pick = 0; pick < maxPicks && maxDifficulty > difficulty; pick++)
        {
            var objective = _objectives.GetRandomObjective(mindId, mind, "TraitorObjectiveGroups");

            if (objective == null)
                continue;
            if (_mindSystem.TryAddObjective(mindId, mind, objective))
                difficulty += objective.Difficulty;
        }

        return true;
    }

    /// <summary>
    ///     Send a codewords and uplink codes to traitor chat.
    /// </summary>
    /// <param name="mind">A mind (player)</param>
    /// <param name="codewords">Codewords</param>
    /// <param name="code">Uplink codes</param>
    private void SendTraitorBriefing(EntityUid mind, string[] codewords, Note[] code)
    {
        if (_mindSystem.TryGetSession(mind, out var session))
        {
           _chatManager.DispatchServerMessage(session, Loc.GetString("traitor-role-greeting"));
           _chatManager.DispatchServerMessage(session, Loc.GetString("traitor-role-codewords", ("codewords", string.Join(", ", codewords))));
           _chatManager.DispatchServerMessage(session, Loc.GetString("traitor-role-uplink-code", ("code", string.Join("-", code).Replace("sharp","#"))));
        }
    }

    private void HandleLatejoin(PlayerSpawnCompleteEvent ev)
    {
        var query = EntityQueryEnumerator<TraitorRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var traitor, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            if (traitor.TotalTraitors >= MaxTraitors)
                continue;
            if (!ev.LateJoin)
                continue;
            if (!ev.Profile.AntagPreferences.Contains(traitor.TraitorPrototypeId))
                continue;

            if (ev.JobId == null || !_prototypeManager.TryIndex<JobPrototype>(ev.JobId, out var job))
                continue;

            if (!job.CanBeAntag)
                continue;

            // Before the announcement is made, late-joiners are considered the same as players who readied.
            if (traitor.SelectionStatus < TraitorRuleComponent.SelectionState.SelectionMade)
            {
                traitor.StartCandidates[ev.Player] = ev.Profile;
                continue;
            }

            // the nth player we adjust our probabilities around
            var target = PlayersPerTraitor * traitor.TotalTraitors + 1;

            var chance = 1f / PlayersPerTraitor;

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
                MakeTraitor(ev.Player);
            }
        }
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        var query = EntityQueryEnumerator<TraitorRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var traitor, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            var result = Loc.GetString("traitor-round-end-result", ("traitorCount", traitor.TraitorMinds.Count));

            result += "\n" + Loc.GetString("traitor-round-end-codewords", ("codewords", string.Join(", ", traitor.Codewords))) +
                      "\n";

            foreach (var mindId in traitor.TraitorMinds)
            {
                if (!TryComp(mindId, out MindComponent? mind))
                    continue;

                var name = mind.CharacterName;
                _mindSystem.TryGetSession(mindId, out var session);
                var username = session?.Name;

                var objectives = mind.AllObjectives.ToArray();
                if (objectives.Length == 0)
                {
                    if (username != null)
                    {
                        if (name == null)
                            result += "\n" + Loc.GetString("traitor-user-was-a-traitor", ("user", username));
                        else
                            result += "\n" + Loc.GetString("traitor-user-was-a-traitor-named", ("user", username),
                                ("name", name));
                    }
                    else if (name != null)
                        result += "\n" + Loc.GetString("traitor-was-a-traitor-named", ("name", name));

                    continue;
                }

                if (username != null)
                {
                    if (name == null)
                        result += "\n" + Loc.GetString("traitor-user-was-a-traitor-with-objectives",
                            ("user", username));
                    else
                        result += "\n" + Loc.GetString("traitor-user-was-a-traitor-with-objectives-named",
                            ("user", username), ("name", name));
                }
                else if (name != null)
                    result += "\n" + Loc.GetString("traitor-was-a-traitor-with-objectives-named", ("name", name));

                foreach (var objectiveGroup in objectives.GroupBy(o => o.Prototype.Issuer))
                {
                    result += "\n" + Loc.GetString($"preset-traitor-objective-issuer-{objectiveGroup.Key}");

                    foreach (var objective in objectiveGroup)
                    {
                        foreach (var condition in objective.Conditions)
                        {
                            var progress = condition.Progress;
                            if (progress > 0.99f)
                            {
                                result += "\n- " + Loc.GetString(
                                    "traitor-objective-condition-success",
                                    ("condition", condition.Title),
                                    ("markupColor", "green")
                                );
                            }
                            else
                            {
                                result += "\n- " + Loc.GetString(
                                    "traitor-objective-condition-fail",
                                    ("condition", condition.Title),
                                    ("progress", (int) (progress * 100)),
                                    ("markupColor", "red")
                                );
                            }
                        }
                    }
                }
            }

            ev.AddLine(result);
        }
    }

    public List<(EntityUid Id, MindComponent Mind)> GetOtherTraitorMindsAliveAndConnected(MindComponent ourMind)
    {
        List<(EntityUid Id, MindComponent Mind)> allTraitors = new();
        foreach (var traitor in EntityQuery<TraitorRuleComponent>())
        {
            foreach (var role in GetOtherTraitorMindsAliveAndConnected(ourMind, traitor))
            {
                if (!allTraitors.Contains(role))
                    allTraitors.Add(role);
            }
        }

        return allTraitors;
    }

    private List<(EntityUid Id, MindComponent Mind)> GetOtherTraitorMindsAliveAndConnected(MindComponent ourMind, TraitorRuleComponent component)
    {
        var traitors = new List<(EntityUid Id, MindComponent Mind)>();
        foreach (var traitor in component.TraitorMinds)
        {
            if (TryComp(traitor, out MindComponent? mind) &&
                mind.OwnedEntity != null &&
                mind.Session != null &&
                mind != ourMind &&
                _mobStateSystem.IsAlive(mind.OwnedEntity.Value) &&
                mind.CurrentEntity == mind.OwnedEntity)
            {
                traitors.Add((traitor, mind));
            }
        }

        return traitors;
    }
}
