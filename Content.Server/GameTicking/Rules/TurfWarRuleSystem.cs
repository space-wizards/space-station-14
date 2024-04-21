using Content.Server.Antag;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Objectives;
using Content.Server.Roles;
using Content.Server.Station.Systems;
using Content.Shared.Doors.Components;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.SprayPainter.Components;
using Content.Shared.SprayPainter.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.GameTicking.Rules;

public sealed class TurfWarRuleSystem : GameRuleSystem<TurfWarRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antagSelection = default!;
    [Dependency] private readonly IChatManager _chatMan = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly TraitorRuleSystem _traitorRule = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayersSpawned);

        SubscribeLocalEvent<TurfWarRuleComponent, ObjectivesTextPrependEvent>(OnObjectivesTextPrepend);
        SubscribeLocalEvent<TurfWarRuleComponent, ObjectivesTextGetInfoEvent>(OnObjectivesTextGetInfo);

        SubscribeLocalEvent<TurfTaggerRoleComponent, GetBriefingEvent>(OnGetBriefing);
    }

    private void OnPlayersSpawned(RulePlayerJobsAssignedEvent args)
    {
        var query = EntityQueryEnumerator<TurfWarRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var comp, out var rule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, rule))
                continue;

            PickTaggers(uid, comp, rule, ref args);
        }
    }

    private void PickTaggers(EntityUid uid, TurfWarRuleComponent comp, GameRuleComponent rule, ref RulePlayerJobsAssignedEvent args)
    {
        var target = Math.Min(args.Players.Length / comp.PlayersPerTagger, comp.Max);
        // if target is below minimum then can't start
        if (target < comp.Min)
        {
            Log.Info($"Not enough players to start a turf war! Only {target} taggers would be selected, minimum is {comp.Min}");
            GameTicker.EndGameRule(uid, rule);
            return;
        }

        var candidates = new Dictionary<ICommonSession, HumanoidCharacterProfile>();
        foreach (var player in args.Players)
        {
            if (!args.Profiles.TryGetValue(player.UserId, out var profile))
                continue;

            candidates[player] = profile;
        }

        // group all players by their department
        var taggerPool = _antagSelection.GetEligiblePlayers(candidates, comp.Antag, includeAllJobs: true);
        var departments = new Dictionary<string, List<EntityUid>>();
        foreach (var mob in taggerPool)
        {
            if (_mind.GetMind(mob) is not {} mind)
                continue;

            // don't pick antagonists of christmas present
            if (_role.MindIsAntagonist(mind))
                continue;

            if (!_job.MindTryGetJob(mind, out _, out var job)
                || !_job.TryGetPrimaryDepartment(job.ID, out var department))
                continue;

            var station = _station.GetOwningStation(mob);
            if (comp.Station == null)
            {
                // get the station from the first player
                comp.Station = station;
            }
            else if (station != comp.Station)
            {
                // only use players from the same station, if that ever happens
                continue;
            }

            if (departments.TryGetValue(department.ID, out var list))
                list.Add(mind);
            else
                departments[department.ID] = new() { mind };
        }

        // if number of departments is below the minimum then can't start
        if (departments.Count < comp.Min)
        {
            Log.Info($"Not unique departments to start a turf war! Only {departments.Count} were found, minimum is {comp.Min}");
            GameTicker.EndGameRule(uid, rule);
            return;
        }

        // pick a random player from each department until theres enough people
        target = Math.Min(target, departments.Count);
        for (int i = 0; i < target; i++)
        {
            var department = _random.Pick(departments.Keys);
            var mind = _random.Pick(departments[department]);
            departments.Remove(department);

            comp.Minds[department] = mind;
        }

        var traitorRules = EntityQuery<TraitorRuleComponent>().ToList();

        // make everyone selected a turf tagger!
        foreach (var (department, mind) in comp.Minds)
        {
            MakeTagger((uid, comp), department, mind);
        }

        Log.Info($"Turf war started on station {comp.Station}");
    }

    /// <summary>
    /// Make a mind a turf tagger.
    /// Not added to a rule's <c>Minds</c> so you have to do that yourself.
    /// </summary>
    public void MakeTagger(Entity<TurfWarRuleComponent> rule, string department, EntityUid mindId, MindComponent? mind = null)
    {
        if (!Resolve(mindId, ref mind) || mind.Session == null)
            return;

        if (mind.OwnedEntity is not {} mob)
            return;

        _role.MindAddRole(mindId, new TurfTaggerRoleComponent(rule, department), mind);

        _mind.TryAddObjective(mindId, mind, rule.Comp.Objective);

        _inventory.SpawnItemsOnEntity(mob, rule.Comp.StartingGear);

        _role.MindPlaySound(mindId, rule.Comp.GreetingSound, mind);
        _chatMan.DispatchServerMessage(mind.Session, Loc.GetString("turf-tagger-role-greeting"));
    }

    /// <summary>
    /// Counts the department-styled airlocks on a station.
    /// </summary>
    public Dictionary<string, int> CountAirlocks(TurfWarRuleComponent rule)
    {
        var counts = new Dictionary<string, int>();
        if (rule.Station == null)
        {
            Log.Error("Tried to count airlocks for a turf war rule with no station");
            return counts;
        }

        var query = EntityQueryEnumerator<PaintableAirlockComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var paint, out var xform))
        {
            if (paint.Department is not {} department)
                continue;

            // ignore doors from departments with no taggers
            if (!rule.Minds.ContainsKey(department))
                continue;

            // airlock must be on the station, not a shittle
            if (_station.GetOwningStation(uid, xform) != rule.Station)
                continue;

            if (counts.TryGetValue(department, out var count))
                counts[department] = count + 1;
            else
                counts[department] = 1;
        }

        return counts;
    }

    private void OnObjectivesTextPrepend(Entity<TurfWarRuleComponent> ent, ref ObjectivesTextPrependEvent args)
    {
        // sort departments by how many airlocks they have, descending
        var counts = CountAirlocks(ent.Comp);
        var winners = new List<string>(counts.Keys);
        winners.Sort((a, b) => counts[b].CompareTo(counts[a]));

        args.Text += "\n" + Loc.GetString("turf-war-round-end-header");
        for (int i = 0; i < winners.Count; i++)
        {
            var department = winners[i];
            var count = counts[department];
            var name = Loc.GetString($"department-{department}");
            args.Text += "\n" + Loc.GetString("turf-war-round-end-result", ("place", i + 1), ("department", name), ("count", count));
        }
    }

    private void OnObjectivesTextGetInfo(Entity<TurfWarRuleComponent> ent, ref ObjectivesTextGetInfoEvent args)
    {
        args.Minds = new List<EntityUid>(ent.Comp.Minds.Values);
        args.AgentName = Loc.GetString("turf-war-round-end-agent-name");
        args.HideObjectives = true;
    }

    private void OnGetBriefing(Entity<TurfTaggerRoleComponent> ent, ref GetBriefingEvent args)
    {
        var name = Loc.GetString($"department-{ent.Comp.Department}");
        args.Append(Loc.GetString("turf-tagger-role-briefing", ("department", name)));
    }
}
