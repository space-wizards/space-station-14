using Content.Server.Objectives.Components;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;
using System.Linq;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;
using System.Diagnostics;

namespace Content.Server.Objectives.Systems;

/// <summary>
///     Handles the logic for the intercept document objective.
/// </summary>
public sealed class InterceptDocumentConditionSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;
    [Dependency] private readonly ObjectivesSystem _serverObjectivesSystem = default!;
    [Dependency] private readonly TargetObjectiveSystem _target = default!;
    [Dependency] private readonly StealConditionSystem _stealConditionSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly HoldDocumentConditionSystem _holdDocSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<InterceptDocumentObjectiveComponent, ObjectiveAssignedEvent>(OnTraitorAssigned, before: new[] { typeof(StealConditionSystem) });
        SubscribeLocalEvent<InterceptDocumentObjectiveComponent, ObjectiveAddedToMindEvent>(OnAddedToMind, after: new[] { typeof(GiveItemsForObjectiveSystem) });

    }

    private void OnTraitorAssigned(EntityUid uid, InterceptDocumentObjectiveComponent comp, ref ObjectiveAssignedEvent args)
    {
        if (args.Cancelled == true)
            return;

        if (!TryComp<TargetObjectiveComponent>(uid, out var selfTargetComp) || !TryComp<StealConditionComponent>(uid, out var selfStealCondComp))
        {
            args.Cancelled = true;
            Debug.Fail($"Missing components for {uid}.");
            return;
        }

        if (!_holdDocSystem.GetAllOtherValidDocHoldObjectives(args.Mind, out var validDocHoldTratorObjectives))
        {
            args.Cancelled = true;
            return;
        }

        var selectedTratorAndObjective = _random.Pick(validDocHoldTratorObjectives);
        var otherTrator = selectedTratorAndObjective.Item1;
        var otherDocHoldObjective = selectedTratorAndObjective.Item2;

        if (!TryComp<StealConditionComponent>(otherDocHoldObjective, out var otherStealCondComp))
        {
            args.Cancelled = true;
            Debug.Fail($"Missing StealConditionComponent for {otherDocHoldObjective}.");
            return;
        }

        if (otherStealCondComp.StealGroup == null)
        {
            args.Cancelled = true;
            Debug.Fail($"StealGroup is null.");
            return;
        }

        // At this point, both traitors steal objectives will be for the same thing.
        // We also wont be changing anything about the other trators objective at this moment. This is done later on.
        _stealConditionSystem.UpdateStealCondition((uid, selfStealCondComp), otherStealCondComp.StealGroup.Value);
        _target.SetTarget(uid, otherTrator, selfTargetComp);
    }

    private void OnAddedToMind(Entity<InterceptDocumentObjectiveComponent> entity, ref ObjectiveAddedToMindEvent args)
    {
        if (!TryComp<StealConditionComponent>(entity, out var stealConditionComp))
            throw new Exception($"Missing StealConditionComponent for {entity}.");

        if (stealConditionComp.StealGroup == null)
            throw new Exception($"StealGroup is null");

        if (!TryComp<TargetObjectiveComponent>(entity, out var targetObjComp))
            throw new Exception($"Missing TargetObjectiveComponent for {entity}.");

        _serverObjectivesSystem.GetObjectives(targetObjComp.Target, "DocHoldObjective", out var objectives);

        if (objectives.Count > 1)
            throw new Exception($"Too many DocHoldObjective for {targetObjComp.Target}.");

        var docHoldObjective = objectives.Single();

        if (!TryComp<HoldDocumentObjectiveComponent>(docHoldObjective, out var otherHoldDocObjectiveComp))
            throw new Exception($"Missing HoldDocumentObjectiveComponent for {docHoldObjective}.");

        otherHoldDocObjectiveComp.IsAvailable = false;

        var targetNameAndJob = _serverObjectivesSystem.TryGetJobAndName(targetObjComp.Target);
        var targetName = targetNameAndJob.Item1;
        var targetJob = targetNameAndJob.Item2;

        var group = _proto.Index(stealConditionComp.StealGroup.Value);
        var name = Loc.GetString(group.Name);

        var title = Loc.GetString(entity.Comp.Title, ("docname", name));
        var description = Loc.GetString(entity.Comp.Description, ("target", targetName), ("taretjob", targetJob), ("docname", name));

        _metaDataSystem.SetEntityName(entity, title);
        _metaDataSystem.SetEntityDescription(entity, description);
        _objectives.SetIcon(entity, group.Sprite);
    }
}
