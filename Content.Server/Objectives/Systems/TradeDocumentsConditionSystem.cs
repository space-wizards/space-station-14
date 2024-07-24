using Content.Server.Objectives.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;
using Robust.Shared.Random;
using System.Linq;
using Content.Server.Objectives.Components.Targets;
using Robust.Shared.Prototypes;
using System.Diagnostics;

namespace Content.Server.Objectives.Systems;

/// <summary>
///     Handles the logic for the trade document objective.
/// </summary>
public sealed class TradeDocumentsConditionSystem : EntitySystem
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
        SubscribeLocalEvent<TradeDocumentsConditionComponent, ObjectiveAssignedEvent>(OnTraitorAssigned);
        SubscribeLocalEvent<TradeDocumentsConditionComponent, ObjectiveItemGivenEvent>(OnObjectiveItemGivenEvent);
        SubscribeLocalEvent<TradeDocumentsConditionComponent, BeforeObjectiveItemGivenEvent>(OnBeforeObjectiveItemGivenEvent);
        SubscribeLocalEvent<TradeDocumentsConditionComponent, ObjectiveAfterAssignEvent>(OnAfterAssign, after: new[] { typeof(StealConditionSystem) });

    }

    private void OnTraitorAssigned(EntityUid uid, TradeDocumentsConditionComponent comp, ref ObjectiveAssignedEvent args)
    {
        if (!TryComp<TargetObjectiveComponent>(uid, out var selfTargetComp) || !TryComp<StealConditionComponent>(uid, out var selfStealCondComp))
        {
            args.Cancelled = true;
            Debug.Fail($"Missing components for {uid}.");
            return;
        }

        var docHoldTratorObjectives = _serverObjectivesSystem.GetAllOtherTratorsWithObjective(args.Mind, "DocHoldObjective");
        var validDocHoldTratorObjectives = new List<(EntityUid, EntityUid)>();
        foreach (var tratorAndObjective in docHoldTratorObjectives)
            if (TryComp<HoldDocumentObjectiveComponent>(tratorAndObjective.Item2, out var holdDocObjComp) && holdDocObjComp.CanBeTraded)
                validDocHoldTratorObjectives.Add(tratorAndObjective);

        // No valid traitors!
        if (validDocHoldTratorObjectives.Count < 1)
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
            Debug.Fail($"Missing components for {otherDocHoldObjective}.");
            return;
        }

        // At this point, both traitors steal objectives will be for the same thing.
        // We also wont be chaning anything about the other trators objective at this moment. This is done later on.
        _stealConditionSystem.UpdateStealCondition((uid, selfStealCondComp), otherStealCondComp.StealGroup);
        _target.SetTarget(uid, otherTrator, selfTargetComp);

    }

    private void OnBeforeObjectiveItemGivenEvent(Entity<TradeDocumentsConditionComponent> entity, ref BeforeObjectiveItemGivenEvent args)
    {
        // All this is to make sure the documents you get aren't the same ones as the person your trading with.

        if (!TryComp<StealConditionComponent>(entity, out var selfStealComp))
            throw new Exception($"Missing StealConditionComponent for {entity}.");
        if (!TryComp<StealTargetComponent>(args.ItemUid, out var stealTargetComp))
            throw new Exception($"Missing StealTargetComponent for {args.ItemUid}.");

        // This works because at this point both the person we are both us and the person we are trading with have the same steal target.
        // This is a little cursed...
        if (selfStealComp.StealGroup == stealTargetComp.StealGroup)
            args.Retry = true;

        return;
    }

    private void OnObjectiveItemGivenEvent(Entity<TradeDocumentsConditionComponent> entity, ref ObjectiveItemGivenEvent args)
    {
        if (!TryComp<TargetObjectiveComponent>(entity, out var targetComp))
            throw new Exception($"Missing TargetObjectiveComponent for {entity}.");
        if (!TryComp<StealTargetComponent>(args.ItemUid, out var stealTargetComp))
            throw new Exception($"Missing StealTargetComponent for {args.ItemUid}.");

        var otherMindId = targetComp.Target;

        if (otherMindId == null)
            throw new Exception($"Mind ID is null.");
        if (!TryComp<MindComponent>(otherMindId, out var otherMindComp))
            throw new Exception($"Missing MindComponent for {otherMindId}.");

        foreach (var objective in otherMindComp.Objectives)
        {
            if (!TryComp<HoldDocumentObjectiveComponent>(objective, out var otherHoldDocObjectiveComp))
                continue;

            if (!TryComp<StealConditionComponent>(objective, out var otherStealCondComp))
                continue;

            otherHoldDocObjectiveComp.CanBeTraded = false;
            _stealConditionSystem.UpdateStealConditionNotify((objective, otherStealCondComp), stealTargetComp.StealGroup, otherMindId.Value);
        }

    }

    private void OnAfterAssign(Entity<TradeDocumentsConditionComponent> entity, ref ObjectiveAfterAssignEvent args)
    {
        if (!TryComp<StealConditionComponent>(entity, out var stealConditionComp))
            throw new Exception($"Missing StealConditionComponent for {entity}.");

        if (!TryComp<TargetObjectiveComponent>(entity, out var targetObjComp))
            throw new Exception($"Missing TargetObjectiveComponent for {entity}.");

        _serverObjectivesSystem.GetObjectives(targetObjComp.Target, "DocHoldObjective", out var objectives);

        if (objectives.Count != 1)
            throw new Exception($"Invalid number DocHoldObjectives on {targetObjComp.Target}.");

        var otherDocHoldObjective = objectives.Single();

        if (!TryComp<StealConditionComponent>(otherDocHoldObjective, out var otherStealConditionComp))
            throw new Exception($"Missing StealConditionComponent for {otherDocHoldObjective}.");

        var selfStealGroup = _proto.Index(stealConditionComp.StealGroup);
        var otherStealGroup = _proto.Index(otherStealConditionComp.StealGroup);

        // This is changing your own objective.
        var otherJob = _serverObjectivesSystem.TryGetJobAndName(targetObjComp.Target).Item2;
        var selftitle = Loc.GetString(entity.Comp.Title, ("docnameself", otherStealGroup.Name), ("docnameother", selfStealGroup.Name));
        var selfdescription = Loc.GetString(entity.Comp.Description, ("otherjobname", otherJob), ("docnameother", selfStealGroup.Name));

        _metaDataSystem.SetEntityName(entity, selftitle, args.Meta);
        _metaDataSystem.SetEntityDescription(entity, selfdescription, args.Meta);
        _objectives.SetIcon(entity, otherStealGroup.Sprite, args.Objective);

        // This is changing the other agents objective.
        var selfJob = _serverObjectivesSystem.TryGetJobAndName(args.MindId, args.Mind).Item2;
        var title = Loc.GetString(entity.Comp.Title, ("docnameself", selfStealGroup.Name), ("docnameother", otherStealGroup.Name));
        var description = Loc.GetString(entity.Comp.Description, ("otherjobname", selfJob), ("docnameother", otherStealGroup.Name));

        _metaDataSystem.SetEntityName(otherDocHoldObjective, title);
        _metaDataSystem.SetEntityDescription(otherDocHoldObjective, description);
    }
}
