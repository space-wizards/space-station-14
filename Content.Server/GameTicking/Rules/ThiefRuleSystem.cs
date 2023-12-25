using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.Shuttles.Components;
using Content.Server.Roles;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.Humanoid;
using Content.Server.Antag;
using Robust.Server.Audio;

namespace Content.Server.GameTicking.Rules;

public sealed class ThiefRuleSystem : GameRuleSystem<ThiefRuleComponent>
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AntagSelectionSystem _antagSelection = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
    [Dependency] private readonly SharedJobSystem _jobs = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;

    const string bigObjectiveGroup = "ThiefBigObjectiveGroups";
    const string smallObjectiveGroup = "ThiefObjectiveGroups";
    const string escapeObjectiveGroup = "ThiefEscapeObjectiveGroups";

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
            if (!_random.Prob(thief.RuleChance))
            {
                RemComp<ThiefRuleComponent>(uid);
                continue;
            }

            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            foreach (var player in ev.Players)
            {
                if (!ev.Profiles.ContainsKey(player.UserId))
                    continue;

                thief.StartCandidates[player] = ev.Profiles[player.UserId];
            }
            DoThiefStart(thief);
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
            MakeThief(thief);
        }
    }

    public bool MakeThief(ICommonSession thief)
    {
        var thiefRule = EntityQuery<ThiefRuleComponent>().FirstOrDefault();
        if (thiefRule == null)
        {
            GameTicker.StartGameRule("Thief", out var ruleEntity);
            thiefRule = Comp<ThiefRuleComponent>(ruleEntity);
        }

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
            var objective = _objectives.GetRandomObjective(mindId, mind, bigObjectiveGroup);
            if (objective != null)
            {
                _mindSystem.AddObjective(mindId, mind, objective.Value);
                difficulty += Comp<ObjectiveComponent>(objective.Value).Difficulty;
            }
        }

        for (var i = 0; i < thiefRule.MaxStealObjectives && thiefRule.MaxObjectiveDifficulty > difficulty; i++)  // Many small objectives
        {
            var objective = _objectives.GetRandomObjective(mindId, mind, smallObjectiveGroup);
            if (objective == null)
                continue;

            _mindSystem.AddObjective(mindId, mind, objective.Value);
            difficulty += Comp<ObjectiveComponent>(objective.Value).Difficulty;
        }

        //Escape target
        var escapeObjective = _objectives.GetRandomObjective(mindId, mind, escapeObjectiveGroup);
        if (escapeObjective != null)
            _mindSystem.AddObjective(mindId, mind, escapeObjective.Value);

        // Give starting items
        _antagSelection.GiveAntagBagGear(mind.OwnedEntity.Value, thiefRule.StarterItems);

        thiefRule.ThiefMinds.Add(mindId);
        return true;
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
        args.Minds = thiefs.Comp.ThiefMinds;
        args.AgentName = Loc.GetString("thief-round-end-agent-name");
    }
}
