using Content.Server.GameTicking.Rules.Components;
using Content.Server.Objectives.Components;
using Content.Shared.Objectives.Components;
using Content.Server.Objectives;

public sealed class ObjectiveLimitSystem : EntitySystem
{
    [Dependency] private readonly ObjectivesSystem _objectiveSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ObjectiveLimitComponent, RequirementCheckEvent>(OnCheck);
    }

    private void OnCheck(Entity<ObjectiveLimitComponent> ent, ref RequirementCheckEvent args)
    {
        if (args.Cancelled)
            return;

        if (Prototype(ent)?.ID is not {} proto)
        {
            Log.Error($"ObjectiveLimit used for non-prototyped objective {ent}");
            return;
        }

        var remaining = ent.Comp.Limit;
        // all traitor rules are considered
        // maybe this would interfere with multistation stuff in the future but eh
        foreach (var rule in EntityQuery<TraitorRuleComponent>())
        {
            foreach (var mindId in rule.TraitorMinds)
            {
                if (mindId == args.MindId || !_objectiveSystem.GetObjectives(mindId, proto, out var _))
                    continue;

                remaining--;

                // limit has been reached, prevent adding the objective
                if (remaining == 0)
                {
                    args.Cancelled = true;
                    return;
                }
            }
        }
    }
}
