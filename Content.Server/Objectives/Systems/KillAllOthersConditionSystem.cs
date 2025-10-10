using System;
using System.Linq;
using Content.Server.Objectives.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Progress is 1 when all other alive minds are dead (or unrevivable). The owner is excluded.
/// </summary>
public sealed class KillAllOthersConditionSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<KillAllOthersConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(EntityUid uid, KillAllOthersConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        // Get all alive humans
        var minds = _mind.GetAliveHumans();
        var ownerMindId = args.MindId;
        // Exclude the owner of the objective (mind) from the target set
        minds.RemoveWhere(m => m.Owner == ownerMindId);

        if (minds.Count == 0)
        {
            args.Progress = 1f;
            return;
        }

        var total = minds.Count;
        // Count ONLY actually dead characters as satisfied
        var satisfied = minds.Count(m => _mind.IsCharacterDeadIc(m.Comp));

        var progress = total == 0 ? 1f : (float) satisfied / total;
        if (progress < 1f)
            progress = MathF.Min(progress, 0.99f);
        args.Progress = progress;
    }
}
