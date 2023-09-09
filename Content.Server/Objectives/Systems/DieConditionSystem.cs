using Content.Server.Objectives.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

public sealed class DieConditionSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DieConditionComponent, ObjectiveGetInfoEvent>(OnGetInfo);
    }

    private void OnGetInfo(EntityUid uid, DieConditionComponent comp, ref ObjectiveGetInfoEvent args)
    {
        args.Info.Progress = _mind.IsCharacterDeadIc(args.Mind) ? 1f : 0f;
    }
}
