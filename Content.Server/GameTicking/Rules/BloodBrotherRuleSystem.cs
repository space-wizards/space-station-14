using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Shared.Player;
using System.Linq;
using Content.Server.Antag;
using Robust.Server.Audio;
using Content.Shared.Traitor.Components;
using Content.Shared.Mobs.Systems;
using Content.Server.Objectives.Components;
using Content.Server.NPC.Systems;

namespace Content.Server.GameTicking.Rules;

public sealed class BloodBrotherRuleSystem : GameRuleSystem<BloodBrotherRuleComponent>
{
    [Dependency] private readonly BloodBrotherRuleComponent _bloodBroRule = default!;
    [Dependency] private readonly AntagSelectionSystem _antagSelection = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly ChatManager _chatManager = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public bool MakeBloodBrother(ICommonSession sesh)
    {
        if (!_mindSystem.TryGetMind(sesh, out var mindId, out var mind))
            return false;
        if (HasComp<BloodBrotherComponent>(mindId))
            return false;
        if (mind.OwnedEntity is not { })
            return false;

        var traitorRule = EntityQuery<TraitorRuleComponent>().FirstOrDefault();
        if (traitorRule == null)
        {
            GameTicker.StartGameRule("Traitor", out var ruleEntity);
            traitorRule = Comp<TraitorRuleComponent>(ruleEntity);
        }

        _roleSystem.MindAddRole(mindId, new BloodBrotherComponent());
        _npcFaction.RemoveFaction(mindId, "NanoTrasen", false);
        _npcFaction.AddFaction(mindId, "Syndicate");
        _npcFaction.AddFaction(mindId, "BloodBrother");

        if (_mindSystem.TryGetSession(mindId, out var session))
        {
            _audio.PlayGlobal(traitorRule.GreetSoundNotification, sesh);
            _chatManager.DispatchServerMessage(sesh, Loc.GetString("bloodbrother-role-greeting"));
            _chatManager.DispatchServerMessage(session, Loc.GetString("traitor-role-codewords", ("codewords", string.Join(", ", traitorRule.Codewords))));
        }

        // roll absolutely random objectives with no difficulty tweaks
        // because nothing can stop real brotherhood
        for (int i = 0; i < _bloodBroRule.MaxObjectives; i++)
            RollObjective(mindId, mind);



        return true;
    }
    private void RollObjective(EntityUid id, MindComponent mind)
    {
        var objective = _objectives.GetRandomObjective(id, mind, "TraitorObjectiveGroups");

        if (objective == null)
            return;

        var target = Comp<TargetObjectiveComponent>(objective.Value).Target;

        // if objective targeted towards another bloodbro we roll another
        if (target != null && Comp<BloodBrotherComponent>((EntityUid) target) != null)
            RollObjective(id, mind);

        _mindSystem.AddObjective(id, mind, (EntityUid) objective);
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
