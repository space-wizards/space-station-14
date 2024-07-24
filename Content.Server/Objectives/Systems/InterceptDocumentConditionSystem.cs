using Content.Server.Objectives.Components;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;
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

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<InterceptDocumentObjectiveComponent, ObjectiveAssignedEvent>(OnTraitorAssigned, before: new[] { typeof(StealConditionSystem) });
        SubscribeLocalEvent<InterceptDocumentObjectiveComponent, ObjectiveAfterAssignEvent>(OnAfterAssign, after: new[] { typeof(StealConditionSystem) });

    }

    private void OnTraitorAssigned(EntityUid uid, InterceptDocumentObjectiveComponent comp, ref ObjectiveAssignedEvent args)
    {
        if (!TryComp<TargetObjectiveComponent>(uid, out var target) || !TryComp<StealConditionComponent>(uid, out var stealComp))
        {
            args.Cancelled = true;
            Debug.Fail($"Missing components for {uid}.");
            return;
        }

        var validTratorObjectives = _serverObjectivesSystem.GetAllOtherTratorsWithObjective(args.Mind, "DocHoldObjective");

        // No valid traitors!
        if (validTratorObjectives.Count < 1)
        {
            args.Cancelled = true;
            return;
        }

        var selectedTratorAndObjective = _random.Pick(validTratorObjectives);
        var chosenTrator = selectedTratorAndObjective.Item1;
        var chosenObjective = selectedTratorAndObjective.Item2;

        if (!TryComp<StealConditionComponent>(chosenObjective, out var targetStealComp))
        {
            args.Cancelled = true;
            Debug.Fail($"Missing StealConditionComponent for {chosenObjective}.");
            return;
        }

        _stealConditionSystem.UpdateStealCondition((uid, stealComp), targetStealComp.StealGroup);

        _target.SetTarget(uid, chosenTrator, target);
    }

    private void OnAfterAssign(Entity<InterceptDocumentObjectiveComponent> entity, ref ObjectiveAfterAssignEvent args)
    {
        if (!TryComp<StealConditionComponent>(entity, out var stealConditionComp))
            throw new Exception($"Missing StealConditionComponent for {entity}.");

        if (!TryComp<TargetObjectiveComponent>(entity, out var targetObjComp))
            throw new Exception($"Missing TargetObjectiveComponent for {entity}.");

        var targetNameAndJob = _serverObjectivesSystem.TryGetJobAndName(targetObjComp.Target);
        var targetName = targetNameAndJob.Item1;
        var targetJob = targetNameAndJob.Item2;

        var group = _proto.Index(stealConditionComp.StealGroup);

        var title = Loc.GetString(entity.Comp.Title, ("docname", group.Name));
        var description = Loc.GetString(entity.Comp.Description, ("target", targetName), ("taretjob", targetJob), ("docname", group.Name));

        _metaDataSystem.SetEntityName(entity, title, args.Meta);
        _metaDataSystem.SetEntityDescription(entity, description, args.Meta);
        _objectives.SetIcon(entity, group.Sprite, args.Objective);
    }
}
