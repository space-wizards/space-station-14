using Content.Server.Objectives.Components;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles applying the objective component blacklist to the objective entity.
/// </summary>
public sealed class ObjectiveBlacklistRequirementSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ObjectiveBlacklistRequirementComponent, RequirementCheckEvent>(OnCheck);
    }

    private void OnCheck(EntityUid uid, ObjectiveBlacklistRequirementComponent comp, ref RequirementCheckEvent args)
    {
        if (args.Cancelled)
            return;

        if (comp.Blacklist.IsValid(uid, EntityManager))
            args.Cancelled = true;
    }
}
