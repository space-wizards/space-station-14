using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.PDA.Ringer;
using Content.Server.Roles;
using Content.Server.Traitor.Uplink;
using Content.Shared.CCVar;
using Content.Shared.Dataset;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Systems;
using Content.Shared.Objectives.Components;
using Content.Shared.PDA;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Linq;
using System.Text;

namespace Content.Server.GameTicking.Rules;

public sealed class TraitorRuleSystem : GameRuleSystem<TraitorRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antagSelection = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly UplinkSystem _uplink = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
    [Dependency] private readonly SharedJobSystem _jobs = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private int PlayersPerTraitor => _cfg.GetCVar(CCVars.TraitorPlayersPerTraitor);
    private int MaxTraitors => _cfg.GetCVar(CCVars.TraitorMaxTraitors);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayersSpawned);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(HandleLatejoin);

        SubscribeLocalEvent<TraitorRuleComponent, ObjectivesTextGetInfoEvent>(OnObjectivesTextGetInfo);
        SubscribeLocalEvent<TraitorRuleComponent, ObjectivesTextPrependEvent>(OnObjectivesTextPrepend);
    }

    //Set min players on game rule
    protected override void Added(EntityUid uid, TraitorRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        gameRule.MinPlayers = _cfg.GetCVar(CCVars.TraitorMinPlayers);
    }

    protected override void Started(EntityUid uid, TraitorRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
        MakeCodewords(component);
    }

    protected override void ActiveTick(EntityUid uid, TraitorRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        if (component.SelectionStatus < TraitorRuleComponent.SelectionState.Started && component.AnnounceAt < _timing.CurTime)
        {
            DoTraitorStart(component);
            component.SelectionStatus = TraitorRuleComponent.SelectionState.Started;
        }
    }

    /// <summary>
    /// Check for enough players
    /// </summary>
    /// <param name="ev"></param>
    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        TryRoundStartAttempt(ev, Loc.GetString("traitor-title"));
    }

    private void MakeCodewords(TraitorRuleComponent component)
    {
        var codewordCount = _cfg.GetCVar(CCVars.TraitorCodewordCount);
        var adjectives = _prototypeManager.Index<DatasetPrototype>(component.CodewordAdjectives).Values;
        var verbs = _prototypeManager.Index<DatasetPrototype>(component.CodewordVerbs).Values;
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
        var eligiblePlayers = _antagSelection.GetEligiblePlayers(_playerManager.Sessions, component.TraitorPrototypeId);

        if (eligiblePlayers.Count == 0)
            return;

        var traitorsToSelect = _antagSelection.CalculateAntagCount(_playerManager.PlayerCount, PlayersPerTraitor, MaxTraitors);

        var selectedTraitors = _antagSelection.ChooseAntags(traitorsToSelect, eligiblePlayers);

        MakeTraitor(selectedTraitors, component);
    }

    private void OnPlayersSpawned(RulePlayerJobsAssignedEvent ev)
    {
        //Start the timer
        var query = QueryActiveRules();
        while (query.MoveNext(out _, out var comp, out var gameRuleComponent))
        {
            var delay = TimeSpan.FromSeconds(
                _cfg.GetCVar(CCVars.TraitorStartDelay) +
                _random.NextFloat(0f, _cfg.GetCVar(CCVars.TraitorStartDelayVariance)));

            //Set the delay for choosing traitors
            comp.AnnounceAt = _timing.CurTime + delay;

            comp.SelectionStatus = TraitorRuleComponent.SelectionState.ReadyToStart;
        }
    }

    public bool MakeTraitor(List<EntityUid> traitors, TraitorRuleComponent component, bool giveUplink = true, bool giveObjectives = true)
    {
        foreach (var traitor in traitors)
        {
            MakeTraitor(traitor, component, giveUplink, giveObjectives);
        }

        return true;
    }

    public bool MakeTraitor(EntityUid traitor, TraitorRuleComponent component, bool giveUplink = true, bool giveObjectives = true)
    {
        //Grab the mind if it wasnt provided
        if (!_mindSystem.TryGetMind(traitor, out var mindId, out var mind))
            return false;

        if (HasComp<TraitorRoleComponent>(mindId))
        {
            Log.Error($"Player {mind.CharacterName} is already a traitor.");
            return false;
        }

        var briefing = Loc.GetString("traitor-role-codewords-short", ("codewords", string.Join(", ", component.Codewords)));

        Note[]? code = null;
        if (giveUplink)
        {
            // Calculate the amount of currency on the uplink.
            var startingBalance = _cfg.GetCVar(CCVars.TraitorStartingBalance);
            if (_jobs.MindTryGetJob(mindId, out _, out var prototype))
                startingBalance = Math.Max(startingBalance - prototype.AntagAdvantage, 0);

            // creadth: we need to create uplink for the antag.
            // PDA should be in place already
            var pda = _uplink.FindUplinkTarget(traitor);
            if (pda == null || !_uplink.AddUplink(traitor, startingBalance))
                return false;

            // Give traitors their codewords and uplink code to keep in their character info menu
            code = EnsureComp<RingerUplinkComponent>(pda.Value).Code;

            // If giveUplink is false the uplink code part is omitted
            briefing = string.Format("{0}\n{1}", briefing,
                Loc.GetString("traitor-role-uplink-code-short", ("code", string.Join("-", code).Replace("sharp", "#"))));
        }

        _antagSelection.SendBriefing(traitor, GenerateBriefing(component.Codewords, code), null, component.GreetSoundNotification);

        component.TraitorMinds.Add(mindId);

        // Assign traitor roles
        _roleSystem.MindAddRole(mindId, new TraitorRoleComponent
        {
            PrototypeId = component.TraitorPrototypeId
        }, mind, true);
        // Assign briefing
        _roleSystem.MindAddRole(mindId, new RoleBriefingComponent
        {
            Briefing = briefing.ToString()
        }, mind, true);

        // Change the faction
        _npcFaction.RemoveFaction(traitor, component.NanoTrasenFaction, false);
        _npcFaction.AddFaction(traitor, component.SyndicateFaction);

        // Give traitors their objectives
        if (giveObjectives)
        {
            var maxDifficulty = _cfg.GetCVar(CCVars.TraitorMaxDifficulty);
            var maxPicks = _cfg.GetCVar(CCVars.TraitorMaxPicks);
            var difficulty = 0f;
            Log.Debug($"Attempting {maxPicks} objective picks with {maxDifficulty} difficulty");
            for (var pick = 0; pick < maxPicks && maxDifficulty > difficulty; pick++)
            {
                var objective = _objectives.GetRandomObjective(mindId, mind, component.ObjectiveGroup);
                if (objective == null)
                    continue;

                _mindSystem.AddObjective(mindId, mind, objective.Value);
                var adding = Comp<ObjectiveComponent>(objective.Value).Difficulty;
                difficulty += adding;
                Log.Debug($"Added objective {ToPrettyString(objective):objective} with {adding} difficulty");
            }
        }

        return true;
    }

    private void HandleLatejoin(PlayerSpawnCompleteEvent ev)
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out _, out var comp, out _))
        {
            if (comp.TotalTraitors >= MaxTraitors)
                continue;

            if (!ev.LateJoin)
                continue;

            if (!_antagSelection.IsPlayerEligible(ev.Player, comp.TraitorPrototypeId))
                continue;

            //If its before we have selected traitors, continue
            if (comp.SelectionStatus < TraitorRuleComponent.SelectionState.Started)
                continue;

            // the nth player we adjust our probabilities around
            var target = PlayersPerTraitor * comp.TotalTraitors + 1;
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
                MakeTraitor(ev.Mob, comp);
            }
        }
    }

    private void OnObjectivesTextGetInfo(EntityUid uid, TraitorRuleComponent comp, ref ObjectivesTextGetInfoEvent args)
    {
        args.Minds = comp.TraitorMinds;
        args.AgentName = Loc.GetString("traitor-round-end-agent-name");
    }

    private void OnObjectivesTextPrepend(EntityUid uid, TraitorRuleComponent comp, ref ObjectivesTextPrependEvent args)
    {
        args.Text += "\n" + Loc.GetString("traitor-round-end-codewords", ("codewords", string.Join(", ", comp.Codewords)));
    }

    /// <summary>
    /// Start this game rule manually
    /// </summary>
    public TraitorRuleComponent StartGameRule()
    {
        var comp = EntityQuery<TraitorRuleComponent>().FirstOrDefault();
        if (comp == null)
        {
            GameTicker.StartGameRule("Traitor", out var ruleEntity);
            comp = Comp<TraitorRuleComponent>(ruleEntity);
        }

        return comp;
    }

    public void MakeTraitorAdmin(EntityUid entity, bool giveUplink, bool giveObjectives)
    {
        var traitorRule = StartGameRule();
        MakeTraitor(entity, traitorRule, giveUplink, giveObjectives);
    }

    private string GenerateBriefing(string[] codewords, Note[]? uplinkCode)
    {
        var sb = new StringBuilder();
        sb.AppendLine(Loc.GetString("traitor-role-greeting"));
        sb.AppendLine(Loc.GetString("traitor-role-codewords-short", ("codewords", string.Join(", ", codewords))));
        if (uplinkCode != null)
            sb.AppendLine(Loc.GetString("traitor-role-uplink-code-short", ("code", string.Join("-", uplinkCode).Replace("sharp", "#"))));

        return sb.ToString();
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
