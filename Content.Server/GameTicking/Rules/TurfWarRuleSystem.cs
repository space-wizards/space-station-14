using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Objectives;
using Content.Server.Roles;
using Content.Server.Station.Systems;
using Content.Shared.Doors.Components;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.SprayPainter.Components;
using Content.Shared.SprayPainter.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.GameTicking.Rules;

public sealed class TurfWarRuleSystem : GameRuleSystem<TurfWarRuleComponent>
{
    [Dependency] private readonly SharedJobSystem _job = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TurfWarRuleComponent, AfterAntagEntitySelectedEvent>(OnSelected);
        SubscribeLocalEvent<TurfWarRuleComponent, ObjectivesTextPrependEvent>(OnObjectivesTextPrepend);

        SubscribeLocalEvent<TurfTaggerRoleComponent, GetBriefingEvent>(OnGetBriefing);
    }

    private void OnSelected(Entity<TurfWarRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        if (args.Session?.AttachedEntity is {} mob)
            ent.Comp.Station ??= _station.GetOwningStation(mob);

        if (!_mind.TryGetMind(args.Session, out var mindId, out var mind)
            || !_job.MindTryGetJob(mindId, out _, out var job)
            || !_job.TryGetPrimaryDepartment(job.ID, out var department))
            return;

        _role.MindAddRole(mindId, new TurfTaggerRoleComponent(ent, department.ID), mind);

        _mind.TryAddObjective(mindId, mind, ent.Comp.Objective);

        ent.Comp.Minds[department.ID] = mindId;
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

    private void OnGetBriefing(Entity<TurfTaggerRoleComponent> ent, ref GetBriefingEvent args)
    {
        var name = Loc.GetString($"department-{ent.Comp.Department}");
        args.Append(Loc.GetString("turf-tagger-role-briefing", ("department", name)));
    }
}
