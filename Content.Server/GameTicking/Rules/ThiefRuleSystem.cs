using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.NPC.Systems;
using Content.Server.Objectives;
using Content.Server.Shuttles.Components;
using Content.Server.Roles;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Objectives.Components;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.CombatMode.Pacification;
using System.Linq;
using Content.Shared.Humanoid;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// Event for spawning a Thief. Auto invoke on start round in Suitable game modes, or can be invoked in mid-game.
/// </summary>
public sealed class ThiefRuleSystem : GameRuleSystem<ThiefRuleComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
    [Dependency] private readonly SharedJobSystem _jobs = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayersSpawned);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(HandleLatejoin);
        SubscribeLocalEvent<ThiefRoleComponent, GetBriefingEvent>(OnGetBriefing);

        SubscribeLocalEvent<ThiefRuleComponent, ObjectivesTextGetInfoEvent>(OnObjectivesTextGetInfo);
    }

    private void OnPlayersSpawned(RulePlayerJobsAssignedEvent ev) //Момент спавна игроков. Инициализация игрового правила.
    {
        var query = EntityQueryEnumerator<ThiefRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var thief, out var gameRule))
        {
            //Chance to not lauch gamerule
            if (!_random.Prob(thief.RuleChance))
            {
                RemComp<ThiefRuleComponent>(uid); //TO DO: NOT TESTED
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
        var thiefPool = FindPotentialThiefs(component.StartCandidates, component);
        var selectedThieves = PickThieves(_random.Next(1,startThiefCount), thiefPool);

        foreach(var thief in selectedThieves)
        {
            MakeThief(thief);
        }
    }

    //TO DO TEST IT!!!
    /// <summary>
    /// If there are not enough thieves in a round after the start of the game, we make the incoming connecting players thieves
    /// </summary>
    private void HandleLatejoin(PlayerSpawnCompleteEvent ev) //Это кстати сработало и при раундстарт подключении. До OnPlayerSpawned
    {
        var query = EntityQueryEnumerator<ThiefRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var thief, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            if (thief.ThiefMinds.Count >= thief.MaxAllowThief)
                continue;
            if (!ev.LateJoin)
                continue;
            if (!ev.Profile.AntagPreferences.Contains(thief.ThiefPrototypeId))
                continue;
            if (ev.JobId == null || !_prototypeManager.TryIndex<JobPrototype>(ev.JobId, out var job))
                continue;
            if (!job.CanBeAntag)
                continue;

            MakeThief(ev.Player);
        }
    }

    private List<ICommonSession> FindPotentialThiefs(in Dictionary<ICommonSession, HumanoidCharacterProfile> candidates, ThiefRuleComponent component)
    {
        //TO DO: When voxels are added, increase their chance of becoming a thief by 4 times >:)
        var list = new List<ICommonSession>();
        var pendingQuery = GetEntityQuery<PendingClockInComponent>();

        foreach (var player in candidates.Keys)
        {
            // Role prevents antag.
            if (!_jobs.CanBeAntag(player))
                continue;

            // Latejoin
            if (player.AttachedEntity != null && pendingQuery.HasComponent(player.AttachedEntity.Value))
                continue;

            list.Add(player);
        }

        var prefList = new List<ICommonSession>();

        foreach (var player in list)
        {
            //player preferences to play as thief
            var profile = candidates[player];
            if (profile.AntagPreferences.Contains(component.ThiefPrototypeId))
            {
                prefList.Add(player);
            }
        }
        if (prefList.Count == 0)
        {
            Log.Info("Insufficient preferred thiefs, picking at random.");
            prefList = list;
        }
        return prefList;
    }

    private List<ICommonSession> PickThieves(int thiefCount, List<ICommonSession> prefList)
    {
        var results = new List<ICommonSession>(thiefCount);
        if (prefList.Count == 0)
        {
            Log.Info("Insufficient ready players to fill up with thieves, stopping the selection.");
            return results;
        }

        for (var i = 0; i < thiefCount; i++)
        {
            results.Add(_random.PickAndTake(prefList));
            Log.Info("Selected a preferred thief.");
        }
        return results;
    }

    public bool MakeThief(ICommonSession thief, bool addPacified = true)
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

        // Add Pacific
        //if (addPacified)
        //{
        //    if (!TryComp<PacifiedComponent>(mind.OwnedEntity, out var pacific))
        //        AddComp<PacifiedComponent>(mind.OwnedEntity.Value);
        //    //TO DO: Pacifism Implanter??
        //}

        // Notificate player about new role assignment
        if (_mindSystem.TryGetSession(mindId, out var session))
        {
            _audioSystem.PlayGlobal(thiefRule.GreetingSound, session);
            _chatManager.DispatchServerMessage(session, MakeBriefing(mind.OwnedEntity.Value));
        }

        // Give thieves their objectives
        var difficulty = 0f;

        if (_random.Prob(0.5f)) // 50% chance to 1 big objective (structure or animal)
        {
            var objective = _objectives.GetRandomObjective(mindId, mind, "ThiefBigObjectiveGroups");
            if (objective != null)
            {
                _mindSystem.AddObjective(mindId, mind, objective.Value);
                difficulty += Comp<ObjectiveComponent>(objective.Value).Difficulty;
            }
        }

        for (var i = 0; i < thiefRule.MaxStealObjectives && thiefRule.MaxObjectiveDifficulty > difficulty; i++)  // Many small objectives
        {
            var objective = _objectives.GetRandomObjective(mindId, mind, "ThiefObjectiveGroups");
            if (objective == null)
                continue;

            _mindSystem.AddObjective(mindId, mind, objective.Value);
            difficulty += Comp<ObjectiveComponent>(objective.Value).Difficulty;
        }

        //Escape target
        var escapeObjective = _objectives.GetRandomObjective(mindId, mind, "ThiefEscapeObjectiveGroups");
        if (escapeObjective != null)
            _mindSystem.AddObjective(mindId, mind, escapeObjective.Value);

        // Give starting items

        thiefRule.ThiefMinds.Add(mindId);
        return true;
    }

    /// <summary>
    /// Add mind briefing
    /// </summary>
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

        if (HasComp<PacifiedComponent>(thief)) // TO DO - update to pacified implanter?
            briefing = briefing + "\n" + Loc.GetString("thief-role-greeting-pacified");

        //briefing += Loc.GetString("thief-role-greeting-equipment"); //TO DO - equipment setting
        return briefing;
    }

    private void OnObjectivesTextGetInfo(Entity<ThiefRuleComponent> thiefs, ref ObjectivesTextGetInfoEvent args) // TO DO - Fix traitor duplicating in manifest
    {
        args.Minds = thiefs.Comp.ThiefMinds;
        args.AgentName = Loc.GetString("thief-round-end-agent-name");
    }
}
