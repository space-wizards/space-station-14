using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Server.Antag;
using Content.Shared.Traitor.Components;
using Content.Shared.Mobs.Systems;
using Content.Server.Objectives.Components;
using Content.Shared.GameTicking;
using Content.Shared.NPC.Systems;
using System.Text;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Prototypes;

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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodBrotherRuleComponent, AfterAntagEntitySelectedEvent>(AfterAntagSelected);
        SubscribeLocalEvent<BloodBrotherRuleComponent, RoundEndMessageEvent>(OnRoundEnd);
    }

    private void AfterAntagSelected(Entity<BloodBrotherRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        if (args.Session == null) return;

        MakeBloodBrother(args.EntityUid, ent);
    }
    private void OnRoundEnd(Entity<BloodBrotherRuleComponent> ent, ref RoundEndMessageEvent args)
    {
        BloodBrotherRuleComponent.CommonObjectives.Clear();
    }

    public bool MakeBloodBrother(EntityUid uid, BloodBrotherRuleComponent component)
    {
        if (!_mindSystem.TryGetMind(uid, out var mindId, out var mind))
            return false;
        if (HasComp<BloodBrotherComponent>(mindId))
            return false;

        _roleSystem.MindAddRole(mindId, new BloodBrotherComponent());
        component.Minds.Add(mindId);

        _antag.SendBriefing(uid, GetBriefing(component), Color.Crimson, component.GreetingSound);

        _npcFactionSystem.RemoveFaction(mindId, "Nanotrasen", false);
        _npcFactionSystem.AddFaction(mindId, "BloodBrother");

        // roll absolutely random objectives with no difficulty tweaks
        // because no hijacks can stop real brotherhood
        if (BloodBrotherRuleComponent.CommonObjectives.Count > 0)
            foreach (var objective in BloodBrotherRuleComponent.CommonObjectives)
                _mindSystem.AddObjective(mindId, mind, objective);

        for (int i = 0; i < component.MaxObjectives / component.NumberOfAntags; i++)
            BloodBrotherRuleComponent.CommonObjectives.Add(RollObjective(mindId, mind));

        var aliveObj = _objectives.GetRandomObjective(mindId, mind, "BloodbrotherAliveObjective");
        if (aliveObj != null)
            _mindSystem.AddObjective(mindId, mind, (EntityUid) aliveObj);

        return true;
    }

    public string GetBriefing(BloodBrotherRuleComponent component)
    {
        var sb = new StringBuilder();
        sb.AppendLine(Loc.GetString("bloodbrother-briefing-start"));

        if (component.NumberOfAntags < 2)
            sb.AppendLine(Loc.GetString("bloodbrother-briefing-nopartner"));
        else
        {
            foreach (var uid in component.Minds)
            {
                var name = Comp<MetaDataComponent>(uid).EntityName;
                _jobs.MindTryGetJobName(uid, out var job);
                sb.AppendLine(Loc.GetString("bloodbrother-briefing-partner", ("partner", name), ("job", job ?? "Unknown")));
            }
            sb.AppendLine(Loc.GetString("bloodbrother-briefing-partner-end"));
        }

        return sb.ToString();
    }

    private EntityUid RollObjective(EntityUid id, MindComponent mind)
    {
        var objective = _objectives.GetRandomObjective(id, mind, "TraitorObjectiveGroups");

        if (objective == null)
        {
            // NEVER STOP ON ROLLING
            return RollObjective(id, mind);
        }

        var target = Comp<TargetObjectiveComponent>(objective.Value).Target;

        // if objective targeted towards another bloodbro we roll another
        if (target != null && Comp<BloodBrotherComponent>((EntityUid) target) != null)
        {
            return RollObjective(id, mind);
        }

        _mindSystem.AddObjective(id, mind, (EntityUid) objective);
        return (EntityUid)objective;
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
