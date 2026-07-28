// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.Objectives.Systems;
using Content.Server.GameTicking.Rules;
using Content.Shared.Objectives.Components;

namespace Content.Server.DeadSpace.Revolutionary;

/// <summary>
/// Система для отслеживания прогресса убийства командного состава революционерами.
/// </summary>
public sealed class KillCommandStaffConditionSystem : EntitySystem
{
    [Dependency] private readonly NumberObjectiveSystem _number = default!;
    [Dependency] private readonly RevolutionaryRuleSystem _revolutionary = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KillCommandStaffConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(EntityUid uid, KillCommandStaffConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = _revolutionary.GetCommandObjectiveProgress(_number.GetTarget(uid));
    }
}
