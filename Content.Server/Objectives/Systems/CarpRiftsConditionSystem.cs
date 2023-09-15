using Content.Server.Objectives.Components;
using Content.Server.Roles;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

public sealed class CarpRiftsConditionSystem : EntitySystem
{
    [Dependency] private readonly NumberObjectiveSystem _number = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CarpRiftsConditionComponent, ObjectiveGetInfoEvent>(OnGetInfo);
    }

    private void OnGetInfo(EntityUid uid, CarpRiftsConditionComponent comp, ref ObjectiveGetInfoEvent args)
    {
        args.Info.Progress = GetProgress(args.MindId, _number.GetTarget(uid));
    }

    private float GetProgress(EntityUid mindId, int target)
    {
        // prevent divide-by-zero
        if (target == 0)
            return 1f;

        if (!TryComp<DragonRoleComponent>(mindId, out var role))
            return 0f;

        if (role.RiftsCharged >= target)
            return 1f;

        return (float) role.RiftsCharged / (float) target;
    }
}
