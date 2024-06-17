using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Server.Antag;
using Content.Shared.BloodBrother.Components;
using Content.Shared.Mobs.Systems;
using Content.Server.Objectives.Components;
using Content.Shared.NPC.Systems;
using System.Text;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Random;
using Content.Server.Roles;

namespace Content.Server.GameTicking.Rules;

public sealed class BloodBrotherRuleSystem : GameRuleSystem<BloodBrotherRuleComponent>
{
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly NpcFactionSystem _npcFactionSystem = default!;
    [Dependency] private readonly SharedJobSystem _jobs = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private BloodBrotherRuleComponent _ruleComp = null;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodBrotherRuleComponent, AfterAntagEntitySelectedEvent>(AfterAntagSelected);
    }

    private void AfterAntagSelected(Entity<BloodBrotherRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        if (args.Session == null)
            return;

        _ruleComp = ent.Comp;

        MakeBloodBrother(args.EntityUid);
    }

    public bool MakeBloodBrother(EntityUid uid, string teamID = "", SharedBloodBrotherComponent? overwriteTarget = null)
    {
        if (!_mindSystem.TryGetMind(uid, out var mindId, out var mind))
            return false;

        _roleSystem.MindAddRole(mindId, new SharedBloodBrotherComponent());

        var tid = teamID;
        if (string.IsNullOrWhiteSpace(tid))
        {
            tid = _random.Next(1, 65535).ToString();
            _ruleComp.Teams.Add(tid, new());
        }
        _ruleComp.Teams[tid].Add(mindId);

        _antag.SendBriefing(uid, GetBriefing(tid), Color.Crimson, _ruleComp.GreetingSound);

        foreach (var member in _ruleComp.Teams[tid])
        {
            if (!TryComp<RoleBriefingComponent>(member, out var roleBrief))
                _roleSystem.MindAddRole(member, new RoleBriefingComponent { Briefing = GetBriefing(tid, true) }, silent: true);
            else roleBrief.Briefing = GetBriefing(tid, true);
        }

        _npcFactionSystem.RemoveFaction(mindId, "Nanotrasen", false);
        _npcFactionSystem.AddFaction(mindId, "BloodBrother");

        var objectives = Comp<SharedBloodBrotherComponent>(_ruleComp.Teams[tid][0]).Objectives;
        if (objectives == null || objectives.Count == 0)
        {

        }

        var aliveObj = _objectives.GetRandomObjective(mindId, mind, "BloodBrotherAliveObjectiveGroup");
        if (aliveObj != null) _mindSystem.AddObjective(mindId, mind, (EntityUid) aliveObj);
        var escapeObj = _objectives.GetRandomObjective(mindId, mind, "BloodBrotherEscapeObjectiveGroup");
        if (escapeObj != null) _mindSystem.AddObjective(mindId, mind, (EntityUid) escapeObj);

        return true;
    }

    public string GetBriefing(string teamID = "", bool shortBrief = false)
    {
        var sb = new StringBuilder();

        if (_ruleComp.Teams[teamID].Count < 2)
        {
            sb.AppendLine(Loc.GetString("bloodbrother-briefing-start-nopartner"));
            return sb.ToString();
        }
        var members = _ruleComp.Teams[teamID].ToArray();
        var teamleadName = Comp<MetaDataComponent>(members[0]).EntityName;
        var teamleadJob = _jobs.MindTryGetJobName(members[0]);

        sb.Append(Loc.GetString("bloodbrother-briefing-start", ("partner", teamleadName), ("job", teamleadJob)));

        if (shortBrief)
            return sb.ToString(); // that's it.

        sb.Append(Loc.GetString("bloodbrother-briefing-fluff"));

        return sb.ToString();
    }

    private EntityUid? RollObjective(EntityUid id, MindComponent mind)
    {
        var objective = _objectives.GetRandomObjective(id, mind, "BloodBrotherObjectiveGroups");

        if (objective == null)
            return objective;

        var target = Comp<TargetObjectiveComponent>(objective.Value).Target;

        // if objective targeted towards another bloodbro we roll another
        if (target != null && Comp<SharedBloodBrotherComponent>((EntityUid) target) != null)
            return RollObjective(id, mind);

        return objective;
    }
}
