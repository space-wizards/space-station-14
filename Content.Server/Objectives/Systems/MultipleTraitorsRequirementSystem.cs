using Content.Server.GameTicking.Rules;
using Content.Server.Objectives.Components;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles requiring multiple traitors being alive for the objective to be given.
/// </summary>
public sealed class MultipleTraitorsRequirementSystem : EntitySystem
{
    [Dependency] private readonly TraitorRuleSystem _traitorRule = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MultipleTraitorsRequirementComponent, RequirementCheckEvent>(OnCheck);
    }

    private void OnCheck(EntityUid uid, MultipleTraitorsRequirementComponent comp, ref RequirementCheckEvent args)
    {
        if (args.Cancelled)
            return;

        if (_traitorRule.GetOtherTraitorMindsAliveAndConnected(args.Mind).Count < comp.Traitors)
            args.Cancelled = true;
    }
}
