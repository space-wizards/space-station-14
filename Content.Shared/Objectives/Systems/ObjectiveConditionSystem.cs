using Content.Shared.Objectives.Components;

namespace Content.Shared.Objectives.Systems;

/// <summary>
/// Provides default info from <see cref="ObjectiveConditionComponent"/> to <see cref="ObjectiveGetInfoEvent"/>.
/// </summary>
public sealed class ObjectiveConditionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ObjectiveConditionComponent, ConditionGetInfoEvent>(OnGetInfo);
    }

    private void OnGetInfo(EntityUid uid, ObjectiveConditionComponent comp, ref ConditionGetInfoEvent args)
    {
        if (comp.Title != null)
            args.Info.Title = comp.Title;
        if (comp.Description != null)
            args.Info.Description = comp.Description;
        if (comp.Icon != null)
            args.Info.Icon = comp.Icon;
    }
}
