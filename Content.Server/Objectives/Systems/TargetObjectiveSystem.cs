using Content.Server.Objectives.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Provides API for other components and handles setting the title.
/// </summary>
public sealed class TargetObjectiveSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TargetObjectiveComponent, ObjectiveGetInfoEvent>(OnGetInfo);
    }

    private void OnGetInfo(EntityUid uid, TargetObjectiveComponent comp)
    {
        if (GetTarget(uid, out var target, comp))
            return;

        args.Info.Title = GetTitle(target, comp.Title);
    }

    /// <summary>
    /// Sets the Target field for the title and other components to use.
    /// </summary>
    public void SetTarget(EntityUid uid, EntityUid target, TargetObjectiveComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        comp.Target = target;
    }

    /// <summary>
    /// Gets the target from the component.
    /// </summary>
    /// <remarks>
    /// If it is null then the prototype is invalid, just return.
    /// </remarks>
    public bool GetTarget(EntityUid uid, [NotNullWhen(true)] out EntityUid? target, TargetObjectiveComponent? comp = null)
    {
        target = Resolve(uid, ref comp) ? comp.Target : null;
        return target != null;
    }

    private string GetTitle(EntityUid target, string title)
    {
        var targetName = "Unknown";
        if (TryComp<MindComponent>(target, out var mind) && mind.CharacterName != null)
        {
            targetName = mind.CharacterName;
        }

        var jobName = _job.MindTryGetJobName(target);
        return Loc.GetString(title, ("targetName", targetName), ("job", jobName));
    }

}
