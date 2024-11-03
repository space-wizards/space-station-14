using Content.Server.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Shared.Objectives.Components;
using Content.Shared.Vampire.Components;
using Content.Shared.Vampire;

namespace Content.Server.Vampire;

public sealed partial class VampireSystem
{
    [Dependency] private readonly NumberObjectiveSystem _number = default!;

    private void InitializeObjectives()
    {
        SubscribeLocalEvent<BloodDrainConditionComponent, ObjectiveGetProgressEvent>(OnBloodDrainGetProgress);
    }

    private void OnBloodDrainGetProgress(EntityUid uid, BloodDrainConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        var target = _number.GetTarget(uid);
        if (target > 0)
            args.Progress = MathF.Min(comp.BloodDranked / target, 1f);
        else args.Progress = 1f;
    }
}