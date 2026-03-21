using System.Linq;
using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Objectives;
using Content.Server.RoundEnd;
using Content.Shared.Mind;
using Content.Shared.Roles.Components;
using Robust.Shared.Log;
using Content.Shared.GameTicking.Components;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// Ends the round when an Assassin completes all their objectives.
/// </summary>
public sealed class AssassinRuleSystem : GameRuleSystem<AssassinRuleComponent>
{
    private static readonly ISawmill Sawmill = Logger.GetSawmill("assassin-rule");
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AssassinRuleComponent, AfterAntagEntitySelectedEvent>(OnAntagSelected);
        SubscribeLocalEvent<AssassinRuleComponent, ObjectivesTextGetInfoEvent>(OnObjectivesTextGetInfo);
    }

    private void OnAntagSelected(Entity<AssassinRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        // Nothing to do here now - AntagSelection handles assignment. We keep the subscription
        // so game rule gets recognized by tooling and to allow future extensions.
    }

    private void OnObjectivesTextGetInfo(EntityUid uid, AssassinRuleComponent comp, ref ObjectivesTextGetInfoEvent args)
    {
        // Provide the list of assigned antag minds and the agent name for round end summaries.
        var minds = _antag.GetAntagMindEntityUids(uid);
        foreach (var mindUid in minds)
        {
            if (!_mind.TryGetMind(mindUid, out var mindId, out var mindComp))
                continue;

            // name for round-end object text - try to get a display name
            var name = "";
            if (mindComp.OriginalOwnerUserId != null && _mind.TryGetMind(mindUid, out var _, out var _))
            {
                // leave name blank - ObjectivesSystem will call GetTitle
            }

            args.Minds.Add((mindId, name));
        }
        args.AgentName = Loc.GetString("assassin-round-end-agent-name");
    }

    protected override void ActiveTick(EntityUid uid, AssassinRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        
        // only run when scheduled
        var now = Timing.CurTime;
        if (component.NextRoundEndCheck.HasValue && component.NextRoundEndCheck > now)
            return;

        component.NextRoundEndCheck = now + component.EndCheckDelay;

        // Iterate assigned antag minds for this rule and check their objectives
        var antagMinds = _antag.GetAntagMinds(uid);
        Sawmill.Debug($"Found {antagMinds.Count} antag minds for {ToPrettyString(uid)}");
        foreach (var mindEntity in antagMinds)
        {
            var mindId = mindEntity.Owner;
            // If the mind has no objectives, skip
            if (mindEntity.Comp.Objectives.Count == 0)
                continue;

            var allCompleted = true;
            foreach (var obj in mindEntity.Comp.Objectives)
            {
                if (!_objectives.IsCompleted(obj, mindEntity))
                {
                    allCompleted = false;
                    break;
                }
            }

            if (allCompleted)
            {
                Sawmill.Info($"Assassin mind {ToPrettyString(mindEntity.Owner)} completed all objectives; ending round.");
                // End the round immediately.
                _roundEnd.EndRound();
                return;
            }
        }
    }
}
