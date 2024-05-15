using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.Roles;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Robust.Shared.Random;

namespace Content.Server.GameTicking.Rules;

public sealed class ThiefRuleSystem : GameRuleSystem<ThiefRuleComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThiefRuleComponent, AfterAntagEntitySelectedEvent>(AfterAntagSelected);

        SubscribeLocalEvent<ThiefRoleComponent, GetBriefingEvent>(OnGetBriefing);
        SubscribeLocalEvent<ThiefRuleComponent, ObjectivesTextGetInfoEvent>(OnObjectivesTextGetInfo);
    }

    private void AfterAntagSelected(Entity<ThiefRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        if (!_mindSystem.TryGetMind(args.EntityUid, out var mindId, out var mind))
            return;

        //Generate objectives
        GenerateObjectives(mindId, mind, ent);
        _antag.SendBriefing(args.EntityUid, MakeBriefing(args.EntityUid), null, null);
    }

    private void GenerateObjectives(EntityUid mindId, MindComponent mind, ThiefRuleComponent thiefRule)
    {
        // Give thieves their objectives
        var difficulty = 0f;

        if (_random.Prob(thiefRule.BigObjectiveChance)) // 70% chance to 1 big objective (structure or animal)
        {
            var objective = _objectives.GetRandomObjective(mindId, mind, thiefRule.BigObjectiveGroup);
            if (objective != null)
            {
                _mindSystem.AddObjective(mindId, mind, objective.Value);
                difficulty += Comp<ObjectiveComponent>(objective.Value).Difficulty;
            }
        }

        for (var i = 0; i < thiefRule.MaxStealObjectives && thiefRule.MaxObjectiveDifficulty > difficulty; i++)  // Many small objectives
        {
            var objective = _objectives.GetRandomObjective(mindId, mind, thiefRule.SmallObjectiveGroup);
            if (objective == null)
                continue;

            _mindSystem.AddObjective(mindId, mind, objective.Value);
            difficulty += Comp<ObjectiveComponent>(objective.Value).Difficulty;
        }

        //Escape target
        var escapeObjective = _objectives.GetRandomObjective(mindId, mind, thiefRule.EscapeObjectiveGroup);
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
        var briefing = isHuman
            ? Loc.GetString("thief-role-greeting-human")
            : Loc.GetString("thief-role-greeting-animal");

        briefing += "\n \n" + Loc.GetString("thief-role-greeting-equipment") + "\n";
        return briefing;
    }

    private void OnObjectivesTextGetInfo(Entity<ThiefRuleComponent> ent, ref ObjectivesTextGetInfoEvent args)
    {
        args.Minds = _antag.GetAntagMindEntityUids(ent.Owner);
        args.AgentName = Loc.GetString("thief-round-end-agent-name");
    }
}
