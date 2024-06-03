using Content.Server.Objectives.Components;
using Content.Shared.Objectives.Components;
using Content.Shared.Whitelist;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles applying the objective component blacklist to the objective entity.
/// </summary>
public sealed class ObjectiveBlacklistRequirementSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ObjectiveBlacklistRequirementComponent, RequirementCheckEvent>(OnCheck);
    }

    private void OnCheck(EntityUid uid, ObjectiveBlacklistRequirementComponent comp, ref RequirementCheckEvent args)
    {
        if (args.Cancelled)
            return;

        foreach (var objective in args.Mind.AllObjectives)
        {
            if (_whitelistSystem.IsBlacklistPass(comp.Blacklist, objective))
            {
                args.Cancelled = true;
                return;
            }
        }
    }
}
