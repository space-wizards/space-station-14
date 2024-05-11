using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Server.Antag;
using Content.Shared.Traitor.Components;
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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodBrotherRuleComponent, AfterAntagEntitySelectedEvent>(AfterAntagSelected);
    }

    private void AfterAntagSelected(Entity<BloodBrotherRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        if (args.Session == null) return;

        MakeBloodBrother(args.EntityUid, ent);
    }

    public bool MakeBloodBrother(EntityUid uid, BloodBrotherRuleComponent component, bool brainwashed = false)
    {
        if (!_mindSystem.TryGetMind(uid, out var mindId, out var mind))
            return false;
        if (HasComp<BloodBrotherComponent>(mindId))
            return false;

        _roleSystem.MindAddRole(mindId, new BloodBrotherComponent());
        component.Minds.Add(mindId);

        //if (component.NumberOfAntags < 2)
        //    _antag.


        _antag.SendBriefing(uid, GetBriefing(component, brainwashed: brainwashed), Color.Crimson, component.GreetingSound);

        if (!TryComp<RoleBriefingComponent>(mindId, out var roleBrief))
            _roleSystem.MindAddRole(mindId, new RoleBriefingComponent { Briefing = GetBriefing(component, brainwashed, true) }, mind, true);
        else roleBrief.Briefing = GetBriefing(component, brainwashed, true);

        _npcFactionSystem.RemoveFaction(mindId, "Nanotrasen", false);
        _npcFactionSystem.AddFaction(mindId, "BloodBrother");

        // roll absolutely random objectives with no difficulty tweaks
        // because no hijacks can stop real brotherhood
        if (component.CommonObjectives.Count > 0)
            foreach (var objective in component.CommonObjectives)
                _mindSystem.AddObjective(mindId, mind, objective);


        if (component.CommonObjectives.Count == 0)
        {
            component.MaxObjectives = _random.Next(1, 3);
            for (int i = 0; i < component.MaxObjectives; i++)
            {
                var objective = RollObjective(mindId, mind);
                if (objective == null) continue;
                if (!component.CommonObjectives.Contains(objective.Value))
                {
                    _mindSystem.AddObjective(mindId, mind, objective.Value);
                    component.CommonObjectives.Add(objective.Value);
                }
            }
        }

        var aliveObj = _objectives.GetRandomObjective(mindId, mind, "BloodBrotherAliveObjectiveGroup");
        if (aliveObj != null) _mindSystem.AddObjective(mindId, mind, (EntityUid) aliveObj);
        var escapeObj = _objectives.GetRandomObjective(mindId, mind, "BloodBrotherEscapeObjectiveGroup");
        if (escapeObj != null) _mindSystem.AddObjective(mindId, mind, (EntityUid) escapeObj);

        return true;
    }

    public string GetBriefing(BloodBrotherRuleComponent component, bool brainwashed = false, bool charMenu = false)
    {
        var sb = new StringBuilder();

        if (charMenu)
        {
            sb.AppendLine(Loc.GetString("bloodbrother-briefing-short"));
            return sb.ToString();
        }

        sb.AppendLine(Loc.GetString(brainwashed ? "bloodbrother-briefing-start-brainwashed" : "bloodbrother-briefing-start"));

        if (component.NumberOfAntags < 2 && !brainwashed)
            sb.AppendLine("\n" + Loc.GetString("bloodbrother-briefing-nopartner"));
        else
        {
            foreach (var uid in component.Minds)
            {
                var name = Comp<MetaDataComponent>(uid).EntityName;
                _jobs.MindTryGetJobName(uid, out var job);
                sb.AppendLine(Loc.GetString("bloodbrother-briefing-partner", ("partner", name), ("job", job ?? "Unknown")));
            }
            sb.AppendLine(Loc.GetString("\n" + "bloodbrother-briefing-partner-end"));
        }

        return sb.ToString();
    }

    private EntityUid? RollObjective(EntityUid id, MindComponent mind)
    {
        var objective = _objectives.GetRandomObjective(id, mind, "BloodBrotherObjectiveGroups");

        if (objective == null)
            return objective;

        var target = Comp<TargetObjectiveComponent>(objective.Value).Target;

        // if objective targeted towards another bloodbro we roll another
        if (target != null && Comp<BloodBrotherComponent>((EntityUid) target) != null)
            return RollObjective(id, mind);

        return objective;
    }
    public List<(EntityUid Id, MindComponent Mind)> GetOtherBroMindsAliveAndConnected(MindComponent ourMind)
    {
        List<(EntityUid Id, MindComponent Mind)> allBros = new();
        foreach (var bro in EntityQuery<BloodBrotherRuleComponent>())
        {
            foreach (var role in GetOtherBroMindsAliveAndConnected(ourMind, bro))
            {
                if (!allBros.Contains(role))
                    allBros.Add(role);
            }
        }

        return allBros;
    }
    private List<(EntityUid Id, MindComponent Mind)> GetOtherBroMindsAliveAndConnected(MindComponent ourMind, BloodBrotherRuleComponent component)
    {
        var bros = new List<(EntityUid Id, MindComponent Mind)>();
        foreach (var bro in component.Minds)
        {
            if (TryComp(bro, out MindComponent? mind) &&
                mind.OwnedEntity != null &&
                mind.Session != null &&
                mind != ourMind &&
                _mobStateSystem.IsAlive(mind.OwnedEntity.Value) &&
                mind.CurrentEntity == mind.OwnedEntity)
            {
                bros.Add((bro, mind));
            }
        }

        return bros;
    }
}
