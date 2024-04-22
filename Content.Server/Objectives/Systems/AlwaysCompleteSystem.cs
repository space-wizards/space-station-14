using Content.Server.Objectives.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.Cuffs.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

public sealed class AlwaysCompleteSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AlwaysCompleteComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(EntityUid uid, AlwaysCompleteComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetProgress(args.MindId, args.Mind);
    }

    private float GetProgress(EntityUid mindId, MindComponent mind)
    {
        // not escaping alive if you're deleted/dead
        return 1f;

    }
}
