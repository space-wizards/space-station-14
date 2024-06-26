using Content.Server.Objectives.Components;
using Content.Shared.Objectives.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles <see cref="CodeConditionComponent"/> progress and provides API for systems to use.
/// </summary>
public sealed class CodeConditionSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CodeConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(Entity<CodeConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = ent.Comp.Completed ? 1f : 0f;
    }

    /// <summary>
    /// Returns whether an objective is completed.
    /// </summary>
    public bool IsCompleted(Entity<CodeConditionComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        return ent.Comp.Completed;
    }

    /// <summary>
    /// Returns true if a mob's objective with a certain prototype is completed.
    /// </summary>
    public bool IsCompleted(Entity<MindContainerComponent?> mob, string prototype)
    {
        if (_mind.GetMind(mob, mob.Comp) is not {} mindId)
            return false;

        if (!_mind.TryFindObjective(mindId, prototype, out var obj))
            return false;

        return IsCompleted(obj.Value);
    }

    /// <summary>
    /// Sets an objective's completed field.
    /// </summary>
    public void SetCompleted(Entity<CodeConditionComponent?> ent, bool completed = true)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.Completed = completed;
    }

    /// <summary>
    /// Sets a mob's objective to complete.
    /// </summary>
    public void SetCompleted(Entity<MindContainerComponent?> mob, string prototype, bool completed = true)
    {
        if (_mind.GetMind(mob, mob.Comp) is not {} mindId)
            return;

        if (!_mind.TryFindObjective(mindId, prototype, out var obj))
            return;

        SetCompleted(obj.Value, completed);
    }
}
