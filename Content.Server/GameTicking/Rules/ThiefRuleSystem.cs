using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.Roles;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.Humanoid;
using Content.Server.Antag;
using Robust.Server.Audio;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Random;

namespace Content.Server.GameTicking.Rules;

public sealed class ThiefRuleSystem : GameRuleSystem<ThiefRuleComponent>
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AntagSelectionSystem _antagSelection = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
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
        var query = EntityQueryEnumerator<ThiefRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var thief, out var gameRule))
        {
            //Chance to not lauch gamerule  
            if (_random.Prob(thief.RuleChance))
            {
                if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                    continue;

                foreach (var player in ev.Players)
                {
                    if (!ev.Profiles.TryGetValue(player.UserId, out var profile))
                        continue;

                    thief.StartCandidates[player] = profile;
                }
                DoThiefStart(thief);
            }
        }
    }

    private void DoThiefStart(ThiefRuleComponent component)
    {
        if (!component.StartCandidates.Any())
        {
            Log.Error("There are no players who can become thieves.");
            return;
        }

        var startThiefCount = Math.Min(component.MaxAllowThief, component.StartCandidates.Count);
        var thiefPool = _antagSelection.FindPotentialAntags(component.StartCandidates, component.ThiefPrototypeId);
        //TO DO: When voxes specifies are added, increase their chance of becoming a thief by 4 times >:)
        var selectedThieves = _antagSelection.PickAntag(_random.Next(1, startThiefCount), thiefPool);

        foreach(var thief in selectedThieves)
        {
            MakeThief(component, thief, component.PacifistThieves);
        }
    }

    public bool MakeThief(ThiefRuleComponent thiefRule, ICommonSession thief, bool addPacified)
    {
        //checks
        if (!_mindSystem.TryGetMind(thief, out var mindId, out var mind))
        {
            Log.Info("Failed getting mind for picked thief.");
            return false;
        }
        if (HasComp<ThiefRoleComponent>(mindId))
        {
            Log.Error($"Player {thief.Name} is already a thief.");
            return false;
        }
        if (mind.OwnedEntity is not { } entity)
        {
            Log.Error("Mind picked for thief did not have an attached entity.");
            return false;
        }

        // Assign thief roles
        _roleSystem.MindAddRole(mindId, new ThiefRoleComponent
        {
            PrototypeId = thiefRule.ThiefPrototypeId
        });

        //Add Pacified  
        //To Do: Long-term this should just be using the antag code to add components.
        if (addPacified) //This check is important because some servers may want to disable the thief's pacifism. Do not remove.
        {
            EnsureComp<PacifiedComponent>(mind.OwnedEntity.Value);
        }

        // Notificate player about new role assignment
        if (_mindSystem.TryGetSession(mindId, out var session))
        {
            _audio.PlayGlobal(thiefRule.GreetingSound, session);
            _chatManager.DispatchServerMessage(session, MakeBriefing(mind.OwnedEntity.Value));
        }

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

        // Give starting items
        _antagSelection.GiveAntagBagGear(mind.OwnedEntity.Value, thiefRule.StarterItems);

        thiefRule.ThievesMinds.Add(mindId);
        return true;
    }

    public void AdminMakeThief(ICommonSession thief, bool addPacified)
    {
        var thiefRule = EntityQuery<ThiefRuleComponent>().FirstOrDefault();
        if (thiefRule == null)
        {
            GameTicker.StartGameRule("Thief", out var ruleEntity);
            thiefRule = Comp<ThiefRuleComponent>(ruleEntity);
        }

        MakeThief(thiefRule, thief, addPacified);
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
        var briefing = "\n";
        briefing = isHuman
            ? Loc.GetString("thief-role-greeting-human")
            : Loc.GetString("thief-role-greeting-animal");

        briefing += "\n \n" + Loc.GetString("thief-role-greeting-equipment") + "\n";
        return briefing;
    }

    private void OnObjectivesTextGetInfo(Entity<ThiefRuleComponent> thiefs, ref ObjectivesTextGetInfoEvent args)
    {
        args.Minds = thiefs.Comp.ThievesMinds;
        args.AgentName = Loc.GetString("thief-round-end-agent-name");
    }
}
