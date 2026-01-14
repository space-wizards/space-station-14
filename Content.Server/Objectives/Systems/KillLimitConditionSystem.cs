using Content.Server.KillTracking;
using Content.Server.Objectives.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Objectives.Components;
using Robust.Shared.Random;

namespace Content.Server.Objectives.Systems;

public sealed class KillLimitConditionSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KillLimitConditionComponent, ObjectiveAssignedEvent>(OnAssigned);
        SubscribeLocalEvent<KillLimitConditionComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);
        SubscribeLocalEvent<KillLimitConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<KillReportedEvent>(OnKillReported);
    }

    private void OnAssigned(Entity<KillLimitConditionComponent> condition, ref ObjectiveAssignedEvent args)
    {
        condition.Comp.PermissibleKillCount = _random.Next(condition.Comp.MinKillCount, condition.Comp.MaxKillCount);
    }
    private void OnAfterAssign(Entity<KillLimitConditionComponent> condition, ref ObjectiveAfterAssignEvent args)
    {
        string title;
        title = Loc.GetString(condition.Comp.ObjectiveTitle, ("limit", condition.Comp.PermissibleKillCount));

        _metaData.SetEntityName(condition.Owner, title, args.Meta);
    }

    private void OnGetProgress(Entity<KillLimitConditionComponent> condition, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = condition.Comp.PermissibleKillCount >= condition.Comp.KillList.Count ? 1f : 0f;

        string description;
        description = Loc.GetString(condition.Comp.ObjectiveDescription, ("limit", condition.Comp.PermissibleKillCount), ("value", condition.Comp.KillList.Count));
        _metaData.SetEntityDescription(condition.Owner, description);
    }

    /// <summary>
    /// Tracks revival of a possible target.
    /// </summary>
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

    private void OnKillReported(ref KillReportedEvent ev)
    {
        if (ev.Primary is KillPlayerSource killer)
        {
            if (_mind.TryGetMind(killer.PlayerId, out var mind) && _mind.TryGetObjectiveComp<KillLimitConditionComponent>(mind.Value.Owner, out var condition, mind.Value.Comp))
                condition.KillList.Add(ev.Entity);
        }
    }
}
