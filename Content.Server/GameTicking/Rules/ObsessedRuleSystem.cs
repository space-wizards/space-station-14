using System.Linq;
using Robust.Shared.Prototypes;
using Content.Server.Antag;
using Robust.Shared.Configuration;
using Content.Shared.CCVar;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Chat.Managers;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.Roles;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared.Objectives.Components;
using Content.Shared.Roles.Jobs;
using Content.Server.Objectives;
using Content.Shared.Mind;
using Content.Server.Objectives.Systems;
using Robust.Server.Player;
using Content.Shared.NPC.Systems;
namespace Content.Server.GameTicking.Rules;


/// <summary>
/// The entrypoint into the Obsessed antag system/code. <br></br>
/// This listens from round-startup, and all Obsessed antag functions basically come from here <br></br>
/// This deals with everything from assigning the right objectives and targets, to enforcing a limit on how many obsesseds there are on the server
/// </summary>
public sealed class ObsessedRuleSystem : GameRuleSystem<ObsessedRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antagSelection = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
    [Dependency] private readonly SharedJobSystem _jobs = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;
    [Dependency] private readonly TargetObjectiveSystem _objectiveSystem = default!; //can use the GetTarget method here to find who the target is

    private int PlayersPerObsessed => _cfg.GetCVar(CCVars.ObsessedRatioOfPlayersToObsessed);
    private int MaxObsessedPlayers => _cfg.GetCVar(CCVars.ObsessedMaxNumberOfAntagsPossibleInRound);


    public override void Initialize()
    {
        base.Initialize();

        //try to make some Obsessed players at round start
        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        //try to make some players obsessed when they spawn in
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayersSpawned);
        //try to make some players obsessed when they spawn in
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(HandleLatejoin);
        //triggered at the round-end, to show the player's objectives
        SubscribeLocalEvent<ObsessedRuleComponent, ObjectivesTextGetInfoEvent>(OnObjectivesTextGetInfo);

    }

    #region Called at round-start or when a new Game Rule is added, handles adding in an Obsessed game rule 
    protected override void ActiveTick(EntityUid uid, ObsessedRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        if (component.SelectionStatus < ObsessedRuleComponent.SelectionState.Started && component.AnnounceAt < _timing.CurTime)
            DoObsessedStart(component);
    }

    private void DoObsessedStart(ObsessedRuleComponent component)
    {
        var eligiblePlayers = _antagSelection.GetEligiblePlayers(_playerManager.Sessions, component.ObsessedPrototypeId);

        if (eligiblePlayers.Count <= 0) //could be an edge-case weh! the number is less than 0
            return;

        var obsessedToSelect = _antagSelection.CalculateAntagCount(_playerManager.PlayerCount, PlayersPerObsessed, MaxObsessedPlayers);

        var selectedObsessed = _antagSelection.ChooseAntags(obsessedToSelect, eligiblePlayers);

        MakeObsessed(selectedObsessed, component);
    }


    public bool MakeObsessed(List<EntityUid> traitors, ObsessedRuleComponent component)
    {
        foreach (var traitor in traitors)
        {
            MakeObsessed(traitor, component);
        }
        return true;
    }
    public void MakeObsessedAdmin(EntityUid entity)
    {
        var traitorRule = StartGameRule(); //todo: need to check with the maintainers if this is ok. Not sure if this will impact other game modes? hasnt caused any bugs for me in load testing, but wondering if it will cause problems on full-server with other antags
        MakeObsessed(entity, traitorRule);
    }


    /// <summary>
    /// Start this game rule manually
    /// </summary>
    public ObsessedRuleComponent StartGameRule()
    {
        var comp = EntityQuery<ObsessedRuleComponent>().FirstOrDefault();
        if (comp == null)
        {
            GameTicker.StartGameRule("Obsessed", out var ruleEntity);
            comp = Comp<ObsessedRuleComponent>(ruleEntity);
        }
        return comp;
    }

    #endregion Called at round-start or when a new Game Rule is added, handles adding in an Obsessed game rule


    private void SendObsessedBriefing(EntityUid mind)
    {
        if (!_mindSystem.TryGetSession(mind, out var session))
            return;

        _chatManager.DispatchServerMessage(session, Loc.GetString("obsessed-role-greeting"));
    }


    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        TryRoundStartAttempt(ev, Loc.GetString("obsessed-title"));
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

            comp.SelectionStatus = ObsessedRuleComponent.SelectionState.ReadyToStart;
        }
    }


    private void HandleLatejoin(PlayerSpawnCompleteEvent ev)
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out _, out var comp, out _))
        {
            if (comp.TotalObsessed >= MaxObsessedPlayers)
                continue;

            if (!ev.LateJoin)
                continue;

            if (!_antagSelection.IsPlayerEligible(ev.Player, comp.ObsessedPrototypeId))
                continue;

            //If its before we have selected traitors, continue
            if (comp.SelectionStatus < ObsessedRuleComponent.SelectionState.Started)
                continue;

            // the nth player we adjust our probabilities around
            var target = PlayersPerObsessed * comp.TotalObsessed + 1;
            var chance = 1f / PlayersPerObsessed;

            // If we have too many obsessed, divide by how many players below target for next traitor we are.
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

            // Now that we've calculated our chance, roll and make them obsessed if we roll under.
            // You get one shot.
            if (_random.Prob(chance))
            {
                MakeObsessed(ev.Mob, comp);
            }
        }
    }

    public bool MakeObsessed(EntityUid obsessedPerson, ObsessedRuleComponent component, bool giveUplink = true, bool giveObjectives = true)
    {
        //Grab the mind and the corresponding uid of the mind.
        if (!_mindSystem.TryGetMind(obsessedPerson, out var mindId, out var mind))
            return false;

        //if they're already obsessed, dont assign new Obsessed antag objectives (They already have objectives!)
        if (HasComp<ObsessedRoleComponent>(mindId))
        {
            Log.Error($"Player {mind.CharacterName} is already an Obsessed.");
            return false;
        }

        //add the actual ObsessedRuleComponent to the player
        //try to add a rulecomponent to the player. had an issue where it sometimes wouldnt do this, so we should explicitly do it (error: Entity does not have a component of type Content.Server.GameTicking.Rules.Components.ObsessedRuleComponent)
        if (!HasComp<ObsessedRuleComponent>(mindId))
            AddComp<ObsessedRuleComponent>(mindId);

        //set up the sound that will play when they're Obsessed
        _roleSystem.MindPlaySound(mindId, component.GreetSoundNotification, mind);

        //add this person to our list of Obsessed antags
        //  we use this to track who exactly is Obsessed - for stats, etc. (like showing names in the round-end summary)
        component.ObsessedMinds.Add(mindId);

        //send the briefing message to them - this will show in their chat window
        SendObsessedBriefing(mindId);

        // Assign traitor roles
        _roleSystem.MindAddRole(mindId, new ObsessedRoleComponent
        {
            PrototypeId = component.ObsessedPrototypeId
        }, mind, true);

        // Assign briefing
        _roleSystem.MindAddRole(mindId, new RoleBriefingComponent
        {
            Briefing = Loc.GetString("obsessed-role-greeting") // this string (and most others) can be found in: SS14\Resources\Locale\en-US\game-ticking\game-presets\preset-obsessed.ftl
        }, mind, true);

        // Change the faction:
        //  not going to be changing the faction. Would upset the main game mode, also doesnt make sense for the obsessed to NOT be an NT employee or passenger, since no other antag can be obsessed

        // Give obsessed their objectives
        if (giveObjectives)
        {
            var maxDifficulty = _cfg.GetCVar(CCVars.ObsessedObjectivesOverallMaxDifficulty);
            var maxPicks = _cfg.GetCVar(CCVars.ObsessedNumberOfObjectivesMaxPicks);
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

    private void OnObjectivesTextGetInfo(EntityUid uid, ObsessedRuleComponent comp, ref ObjectivesTextGetInfoEvent args)
    {
        args.Minds = comp.ObsessedMinds;
        args.AgentName = Loc.GetString("obsessed-round-end-agent-name"); // it says "Obsessed". This isn't the player Faction, it just shows that there were Obsessed players, and who they were.
    }
}
