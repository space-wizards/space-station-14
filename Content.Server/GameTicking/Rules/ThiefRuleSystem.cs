using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.Roles;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Random;
using Content.Shared.Roles;
using Robust.Shared.Random;
using System.Linq;
using System.Text;

namespace Content.Server.GameTicking.Rules;

public sealed class ThiefRuleSystem : GameRuleSystem<ThiefRuleComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AntagSelectionSystem _antagSelection = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;

    [ValidatePrototypeId<WeightedRandomPrototype>]
    const string BigObjectiveGroup = "ThiefBigObjectiveGroups";
    [ValidatePrototypeId<WeightedRandomPrototype>]
    const string SmallObjectiveGroup = "ThiefObjectiveGroups";
    [ValidatePrototypeId<WeightedRandomPrototype>]
    const string EscapeObjectiveGroup = "ThiefEscapeObjectiveGroups";

    private const float BigObjectiveChance = 0.7f;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayersSpawned);

        SubscribeLocalEvent<ThiefRoleComponent, GetBriefingEvent>(OnGetBriefing);
        SubscribeLocalEvent<ThiefRuleComponent, ObjectivesTextGetInfoEvent>(OnObjectivesTextGetInfo);
    }

    private void OnPlayersSpawned(RulePlayerJobsAssignedEvent ev)
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out _, out var comp, out _))
        {
            //Chance to not launch the game rule
            if (!_random.Prob(comp.RuleChance))
                continue;

            //Get all players eligible for this role
            var eligiblePlayers = _antagSelection.GetEligiblePlayers(ev.Players, comp.ThiefPrototypeId);

            //Abort if there are none
            if (eligiblePlayers.Count == 0)
                continue;

            //Calculate number of thieves to choose
            var thiefCount = _random.Next(1, comp.MaxAllowThief + 1);

            //Select our theives
            var thieves = _antagSelection.ChooseAntags(thiefCount, eligiblePlayers);

            MakeThief(thieves, comp, comp.PacifistThieves);
        }
    }

    public void MakeThief(List<EntityUid> players, ThiefRuleComponent thiefRule, bool addPacified)
    {
        foreach (var thief in players)
        {
            MakeThief(thief, thiefRule, addPacified);
        }
    }
    public void MakeThief(EntityUid thief, ThiefRuleComponent thiefRule, bool addPacified)
    {
        if (!_mindSystem.TryGetMind(thief, out var mind, out var mindComponent))
            return;

        // Assign thief roles
        _roleSystem.MindAddRole(mind, new ThiefRoleComponent
        {
            PrototypeId = thiefRule.ThiefPrototypeId,
        }, silent: true);

        //Add Pacified  
        //To Do: Long-term this should just be using the antag code to add components.
        if (addPacified) //This check is important because some servers may want to disable the thief's pacifism. Do not remove.
        {
            EnsureComp<PacifiedComponent>(thief);
        }

        //Generate objectives
        GenerateObjectives(mind, mindComponent, thiefRule);

        //Send briefing here to account for humanoid/animal
        _antagSelection.SendBriefing(thief, MakeBriefing(thief), null, thiefRule.GreetingSound);

        // Give starting items
        _antagSelection.GiveAntagBagGear(thief, thiefRule.StarterItems);

        thiefRule.ThievesMinds.Add(mind);
    }

    public void AdminMakeThief(MindComponent mind, bool addPacified)
    {
        var thiefRule = EntityQuery<ThiefRuleComponent>().FirstOrDefault();
        if (thiefRule == null)
        {
            GameTicker.StartGameRule("Thief", out var ruleEntity);
            thiefRule = Comp<ThiefRuleComponent>(ruleEntity);
        }

        if (!HasComp<ThiefRoleComponent>(mind.OwnedEntity))
        {
            if (mind.OwnedEntity != null)
            {
                MakeThief(mind.OwnedEntity.Value, thiefRule, addPacified);
            }
        }
    }

    private void GenerateObjectives(EntityUid mindId, MindComponent mind, ThiefRuleComponent thiefRule)
    {
        // Give thieves their objectives
        var difficulty = 0f;

        if (_random.Prob(BigObjectiveChance)) // 70% chance to 1 big objective (structure or animal)
        {
            var objective = _objectives.GetRandomObjective(mindId, mind, BigObjectiveGroup);
            if (objective != null)
            {
                _mindSystem.AddObjective(mindId, mind, objective.Value);
                difficulty += Comp<ObjectiveComponent>(objective.Value).Difficulty;
            }
        }

        for (var i = 0; i < thiefRule.MaxStealObjectives && thiefRule.MaxObjectiveDifficulty > difficulty; i++)  // Many small objectives
        {
            var objective = _objectives.GetRandomObjective(mindId, mind, SmallObjectiveGroup);
            if (objective == null)
                continue;

            _mindSystem.AddObjective(mindId, mind, objective.Value);
            difficulty += Comp<ObjectiveComponent>(objective.Value).Difficulty;
        }

        //Escape target
        var escapeObjective = _objectives.GetRandomObjective(mindId, mind, EscapeObjectiveGroup);
        if (escapeObjective != null)
            _mindSystem.AddObjective(mindId, mind, escapeObjective.Value);
    }

    //Add mind briefing
    private void OnGetBriefing(Entity<ThiefRoleComponent> thief, ref GetBriefingEvent args)
    {
        if (!TryComp<MindComponent>(thief.Owner, out var mind) || mind.OwnedEntity == null)
            return;

        args.Append(MakeBriefing(mind.OwnedEntity.Value));
    }

    private string MakeBriefing(EntityUid thief)
    {
        var isHuman = HasComp<HumanoidAppearanceComponent>(thief);
        var sb = new StringBuilder();
        sb.AppendLine(isHuman ? Loc.GetString("thief-role-greeting-human") : Loc.GetString("thief-role-greeting-animal"));
        sb.AppendLine();
        sb.AppendLine(Loc.GetString("thief-role-greeting-equipment"));

        return sb.ToString();
    }

    private void OnObjectivesTextGetInfo(Entity<ThiefRuleComponent> thiefs, ref ObjectivesTextGetInfoEvent args)
    {
        args.Minds = thiefs.Comp.ThievesMinds;
        args.AgentName = Loc.GetString("thief-round-end-agent-name");
    }
}
