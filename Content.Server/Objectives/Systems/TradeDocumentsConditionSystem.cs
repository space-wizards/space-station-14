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
    [Dependency] private readonly HoldDocumentConditionSystem _holdDocSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TradeDocumentsConditionComponent, ObjectiveAssignedEvent>(OnTraitorAssigned, after: new[] { typeof(MultipleTraitorsRequirementSystem) });
        SubscribeLocalEvent<TradeDocumentsConditionComponent, ObjectiveItemGivenEvent>(OnObjectiveItemGivenEvent);
        SubscribeLocalEvent<TradeDocumentsConditionComponent, BeforeObjectiveItemGivenEvent>(OnBeforeObjectiveItemGivenEvent);
        SubscribeLocalEvent<TradeDocumentsConditionComponent, ObjectiveAddedToMindEvent>(OnAddedToMind, after: new[] { typeof(GiveItemsForObjectiveSystem) });

    }

    private void OnTraitorAssigned(EntityUid uid, TradeDocumentsConditionComponent comp, ref ObjectiveAssignedEvent args)
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

    private void OnBeforeObjectiveItemGivenEvent(Entity<TradeDocumentsConditionComponent> entity, ref BeforeObjectiveItemGivenEvent args)
    {
        // All this is to make sure the documents you get aren't the same ones as the person your trading with.

        if (!TryComp<StealConditionComponent>(entity, out var selfStealComp))
            throw new Exception($"Missing StealConditionComponent for {entity}.");
        if (!TryComp<StealTargetComponent>(args.ItemUid, out var stealTargetComp))
            throw new Exception($"Missing StealTargetComponent for {args.ItemUid}.");

        // This works because at this point both is and the person we are trading with have the same steal target.
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

        if (!TryComp<MindComponent>(otherMindId, out var otherMindComp))
            throw new Exception($"Missing MindComponent for {otherMindId}.");

        _serverObjectivesSystem.GetObjectives(otherMindId, "DocHoldObjective", out var objectives, otherMindComp);

        if (objectives.Count > 1)
            throw new Exception($"Too many DocHoldObjective for {otherMindId}.");

        var docHoldObjective = objectives.Single();

        if (!TryComp<StealConditionComponent>(docHoldObjective, out var otherStealCondComp))
            throw new Exception($"Missing StealConditionComponent for {docHoldObjective}.");

        if (!TryComp<HoldDocumentObjectiveComponent>(docHoldObjective, out var otherHoldDocObjectiveComp))
            throw new Exception($"Missing HoldDocumentObjectiveComponent for {docHoldObjective}.");

        otherHoldDocObjectiveComp.IsAvailable = false;
        _stealConditionSystem.UpdateStealConditionNotify((docHoldObjective, otherStealCondComp), stealTargetComp.StealGroup, otherMindId.Value);
    }

    private void OnAddedToMind(Entity<TradeDocumentsConditionComponent> entity, ref ObjectiveAddedToMindEvent args)
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

        if (stealConditionComp.StealGroup == null || otherStealConditionComp.StealGroup == null)
            throw new Exception($"StealGroup is null.");

        var selfStealGroup = _proto.Index(stealConditionComp.StealGroup.Value);
        var otherStealGroup = _proto.Index(otherStealConditionComp.StealGroup.Value);

        var selfName = Loc.GetString(selfStealGroup.Name);
        var otherName = Loc.GetString(otherStealGroup.Name);

        // This is changing your own objective.
        var otherJob = _serverObjectivesSystem.TryGetJobAndName(targetObjComp.Target).Item2;
        var selftitle = Loc.GetString(entity.Comp.Title, ("docnameself", otherName), ("docnameother", selfName));
        var selfdescription = Loc.GetString(entity.Comp.Description, ("otherjobname", otherJob), ("docnameother", selfName));

        _metaDataSystem.SetEntityName(entity, selftitle);
        _metaDataSystem.SetEntityDescription(entity, selfdescription);
        _objectives.SetIcon(entity, otherStealGroup.Sprite);

        // This is changing the other agents objective.
        var selfJob = _serverObjectivesSystem.TryGetJobAndName(args.MindId, args.Mind).Item2;
        var title = Loc.GetString(entity.Comp.Title, ("docnameself", selfName), ("docnameother", otherName));
        var description = Loc.GetString(entity.Comp.Description, ("otherjobname", selfJob), ("docnameother", otherName));

        _metaDataSystem.SetEntityName(otherDocHoldObjective, title);
        _metaDataSystem.SetEntityDescription(otherDocHoldObjective, description);
    }
}
