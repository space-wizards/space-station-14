// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Objectives.Components;
using Content.Server.DeadSpace.Sith.Components;
using Content.Server.Objectives.Systems;

namespace Content.Server.DeadSpace.Sith;

public sealed class SithSubmissionConditionsSystem : EntitySystem
{
    [Dependency] private readonly NumberObjectiveSystem _number = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SithSubmissionConditionsComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(EntityUid uid, SithSubmissionConditionsComponent component, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = SubordinationOfCommandProgress(component, _number.GetTarget(uid));
    }

    private float SubordinationOfCommandProgress(SithSubmissionConditionsComponent component, int target)
    {
        if (target == 0)
            return 1f;

        component.Progress = MathF.Min((float) component.SubordinateCommand.Count / (float) target, 1f);

        return component.Progress;
    }

    public void SubordinationOfCommandCharged(EntityUid uid, EntityUid target, SithSubmissionConditionsComponent? component)
    {
        if (!Resolve(uid, ref component))
            return;

        component.SubordinateCommand.Add(target);
    }

    public bool TryResetSubordination(EntityUid uid, EntityUid target, SithSubmissionConditionsComponent? component)
    {
        if (!Resolve(uid, ref component))
            return false;

        return component.SubordinateCommand.Remove(target);
    }
}
