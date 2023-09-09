using Content.Server.GameTicking.Rules.Components;
using Content.Server.Objectives;
using Content.Server.Terminator.Components;
using Content.Server.Terminator.Systems;
using Robust.Shared.Map;

namespace Content.Server.GameTicking.Rules;

public sealed class TerminatorRuleSystem : GameRuleSystem<TerminatorRuleComponent>
{
    [Dependency] private readonly TerminatorSystem _terminator = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TerminatorRuleComponent, ObjectivesTextGetInfoEvent>(OnObjectivesTextGetInfo);
    }

    private void OnObjectivesTextGetInfo(EntityUid uid, TerminatorRuleComponent comp, ref ObjectivesTextGetInfoEvent args)
    {
        args.Minds = comp.Minds;
        args.AgentName = Loc.GetString("terminator-round-end-agent-name");
    }

    /// <summary>
    /// Start a new rule with a single terminator.
    /// </summary>
    public EntityUid StartRule(EntityUid mind)
    {
        GameTicker.StartGameRule("Terminator", out var ruleId);
        var comp = Comp<TerminatorRuleComponent>(ruleId);
        comp.Minds.Add(mind);
        return ruleId;
    }

    /// <summary>
    /// Add a terminator's mind to this rule's list.
    /// </summary>
    public void AddMind(EntityUid uid, EntityUid mindId, TerminatorRuleComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        comp.Minds.Add(mindId);
    }

    /// <summary>
    /// Create a spawner at a position and return it.
    /// </summary>
    /// <param name="coords">Coordinates to create the spawner at</param>
    /// <param name="target">Optional target mind to force the terminator to target</param>
    public EntityUid CreateSpawner(EntityCoordinates coords, EntityUid? target)
    {
        var uid = Spawn("SpawnPointGhostTerminator", coords);
        if (target != null)
        {
            var comp = EnsureComp<TerminatorTargetComponent>(uid);
            _terminator.SetTarget(uid, target.Value, comp);
        }

        return uid;
    }
}
