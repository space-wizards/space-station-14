using Content.Server.Objectives.Components;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Automatically assign objective as complete.
/// </summary>
public sealed class AutoCompleteConditionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutoCompleteConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(EntityUid uid, AutoCompleteConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = 1f;
    }
}
