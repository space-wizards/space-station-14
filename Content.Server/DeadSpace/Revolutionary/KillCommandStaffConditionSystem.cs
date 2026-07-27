// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.Objectives.Systems;
using Content.Server.Objectives.Components;
using Content.Server.Revolutionary.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Objectives.Components;

namespace Content.Server.DeadSpace.Revolutionary;

/// <summary>
/// Система для отслеживания прогресса убийства командного состава революционерами.
/// </summary>
public sealed class KillCommandStaffConditionSystem : EntitySystem
{
    [Dependency] private readonly NumberObjectiveSystem _number = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KillCommandStaffConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(EntityUid uid, KillCommandStaffConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = CalculateProgress(_number.GetTarget(uid));
    }

    /// <summary>
    /// Reads the configured target and calculates progress without raising objective events or changing objective state.
    /// </summary>
    public bool TryGetProgress(EntityUid objective, out float progress)
    {
        progress = 0f;
        if (!HasComp<KillCommandStaffConditionComponent>(objective) ||
            !HasComp<NumberObjectiveComponent>(objective))
        {
            return false;
        }

        progress = CalculateProgress(_number.GetTarget(objective));
        return true;
    }

    public float CalculateProgress(int targetPercent)
    {
        if (targetPercent <= 0)
            return 1f;

        var totalCount = 0;
        var deadCount = 0;

        var query = EntityQueryEnumerator<CommandStaffComponent>();
        while (query.MoveNext(out var commandUid, out _))
        {
            totalCount++;

            if (IsCommandStaffDead(commandUid))
                deadCount++;
        }

        if (totalCount == 0)
            return 1f;

        var currentPercent = (float)deadCount / totalCount * 100f;
        var progress = Math.Min(currentPercent / targetPercent, 1f);

        return progress;
    }

    private bool IsCommandStaffDead(EntityUid uid)
    {
        // Проверяем состояние моба
        if (_mobState.IsDead(uid))
            return true;

        // Проверяем, удалён ли (gibbed)
        if (Deleted(uid))
            return true;

        // Проверяем через mind если есть
        if (_mind.TryGetMind(uid, out var mindId, out var mind))
        {
            if (_mind.IsCharacterDeadIc(mind))
                return true;
        }

        return false;
    }
}
