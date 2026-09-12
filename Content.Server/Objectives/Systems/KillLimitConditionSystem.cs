using Content.Server.KillTracking;
using Content.Server.Objectives.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Objectives.Components;
using Robust.Shared.Random;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// See <see cref="KillLimitConditionComponent"/>.
/// </summary>
public sealed partial class KillLimitConditionSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private MetaDataSystem _metaData = default!;

    /// <summary> Initializes <see cref="KillLimitConditionComponent.PermissibleKillCount"/>. </summary>
    [SubscribeLocalEvent]
    private void OnAssigned(Entity<KillLimitConditionComponent> condition, ref ObjectiveAssignedEvent args)
    {
        condition.Comp.PermissibleKillCount = _random.Next(condition.Comp.MinKillCount, condition.Comp.MaxKillCount);
    }

    /// <summary> Edits objective name to include selected <see cref="KillLimitConditionComponent.PermissibleKillCount"/>. </summary>
    [SubscribeLocalEvent]
    private void OnAfterAssign(Entity<KillLimitConditionComponent> condition, ref ObjectiveAfterAssignEvent args)
    {
        var title = Loc.GetString(condition.Comp.ObjectiveTitle, ("limit", condition.Comp.PermissibleKillCount));
        _metaData.SetEntityName(condition.Owner, title, args.Meta);
    }

    [SubscribeLocalEvent]
    private void OnGetProgress(Entity<KillLimitConditionComponent> condition, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = condition.Comp.PermissibleKillCount >= condition.Comp.KillList.Count ? 1f : 0f;

        var description = Loc.GetString(
            condition.Comp.ObjectiveDescription,
            ("limit", condition.Comp.PermissibleKillCount),
            ("value", condition.Comp.KillList.Count)
        );
        _metaData.SetEntityDescription(condition.Owner, description);
    }

    /// <summary> Tracks revival of a possible target. </summary>
    [SubscribeLocalEvent]
    private void OnMobStateChanged(MobStateChangedEvent ev)
    {
        if (ev.NewMobState == MobState.Dead && ev.OldMobState != MobState.Dead)
            return;

        var query = EntityQueryEnumerator<KillLimitConditionComponent>();
        while (query.MoveNext(out _, out var comp))
        {
            if (comp.AllowReviving)
                comp.KillList.Remove(ev.Target);
        }
    }

    /// <summary> Adds killed to kills list. </summary>
    [SubscribeLocalEvent]
    private void OnKillReported(ref KillReportedEvent ev)
    {
        if (ev.Primary is not KillPlayerSource killer)
            return;

        if (_mind.TryGetMind(killer.PlayerId, out var mind)
            && _mind.TryGetObjectiveComp<KillLimitConditionComponent>(mind.Value.Owner, out var condition, mind.Value.Comp))
        {
            condition.KillList.Add(ev.Entity);
        }
    }
}
