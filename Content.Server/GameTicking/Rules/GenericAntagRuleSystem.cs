using Content.Server.GameTicking.Rules.Components;
using Content.Server.Objectives;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// Handles round end text for simple antags.
/// Adding objectives is handled in its own system.
/// </summary>
public sealed class GenericAntagRuleSystem : GameRuleSystem<GenericAntagRuleComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GenericAntagRuleComponent, ObjectivesTextGetInfoEvent>(OnObjectivesTextGetInfo);
    }

    /// <summary>
    /// Start a simple antag's game rule.
    /// If it is invalid the rule is deleted and null is returned.
    /// </summary>
    public bool StartRule(string rule, EntityUid mindId, [NotNullWhen(true)] out EntityUid? ruleId, [NotNullWhen(true)] out GenericAntagRuleComponent? comp)
    {
        ruleId = GameTicker.AddGameRule(rule);
        if (!TryComp<GenericAntagRuleComponent>(ruleId, out comp))
        {
            Log.Error($"Simple antag rule prototype {rule} is invalid, deleting it.");
            Del(ruleId);
            ruleId = null;
            return false;
        }

        if (!GameTicker.StartGameRule(ruleId.Value))
        {
            Log.Error($"Simple antag rule prototype {rule} failed to start, deleting it.");
            Del(ruleId);
            ruleId = null;
            comp = null;
            return false;
        }

        comp.Minds.Add(mindId);
        return true;
    }

    private void OnObjectivesTextGetInfo(EntityUid uid, GenericAntagRuleComponent comp, ref ObjectivesTextGetInfoEvent args)
    {
        args.Minds = comp.Minds;
        args.AgentName = Loc.GetString(comp.AgentName);
    }
}
