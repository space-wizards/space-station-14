using Content.Server.Objectives.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Roles.Jobs;
using Robust.Shared.GameObjects;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Provides API for other components and handles setting the title.
/// </summary>
public sealed class TargetObjectiveSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TargetObjectiveComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);
    }

    private void OnAfterAssign(EntityUid uid, TargetObjectiveComponent comp, ref ObjectiveAfterAssignEvent args)
    {
        if (!GetTarget(uid, out var target, comp))
            return;

        if (comp.Title != null)
            _metaData.SetEntityName(uid, Format(target.Value, comp.Title.Value), args.Meta);

        if (comp.Description != null)
        _metaData.SetEntityDescription(uid, Format(target.Value, comp.Description.Value), args.Meta);
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

    /// <summary>
    /// Format either a title or description.
    /// </summary>
    private string Format(EntityUid target, LocId fmt)
    {
        var targetName = "Unknown";
        if (TryComp<MindComponent>(target, out var mind) && mind.CharacterName != null)
        {
            targetName = mind.CharacterName;
        }

        var jobName = _job.MindTryGetJobName(target);
        return Loc.GetString(fmt, ("targetName", targetName), ("job", jobName));
    }

}
