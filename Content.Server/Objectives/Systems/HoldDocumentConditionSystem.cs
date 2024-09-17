using Content.Server.Objectives.Components;
using Content.Server.Objectives.Components.Targets;
using Robust.Shared.Prototypes;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;
using Content.Shared.Mind;

namespace Content.Server.Objectives.Systems;

public sealed class HoldDocumentConditionSystem : EntitySystem
{
    [Dependency] private readonly ObjectivesSystem _serverObjectivesSystem = default!;
    [Dependency] private readonly StealConditionSystem _stealConditionSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HoldDocumentObjectiveComponent, ObjectiveItemGivenEvent>(OnObjectiveItemGivenEvent);
        SubscribeLocalEvent<HoldDocumentObjectiveComponent, ObjectiveAddedToMindEvent>(OnObjectiveAddedToMind, after: new[] { typeof(StealConditionSystem) });
    }

    private void OnObjectiveItemGivenEvent(Entity<HoldDocumentObjectiveComponent> entity, ref ObjectiveItemGivenEvent args)
    {
        if (!TryComp<StealConditionComponent>(entity, out var stealComp))
            throw new Exception($"Missing StealConditionComponent for {entity}.");
        if (!TryComp<StealTargetComponent>(args.ItemUid, out var stealTargetComp) || stealTargetComp == null)
            throw new Exception($"Missing StealTargetComponent for {args.ItemUid}.");

        _stealConditionSystem.UpdateStealCondition((entity, stealComp), stealTargetComp.StealGroup);
    }

    private void OnObjectiveAddedToMind(Entity<HoldDocumentObjectiveComponent> entity, ref ObjectiveAddedToMindEvent args)
    {
        if (!TryComp<StealConditionComponent>(entity, out var stealConditionComp))
            throw new Exception($"Missing StealConditionComponent for {entity}.");

        if (stealConditionComp.StealGroup == null)
            throw new Exception($"StealGroup is null");

        var group = _proto.Index(stealConditionComp.StealGroup.Value);
        var name = Loc.GetString(group.Name);

        var title = Loc.GetString(entity.Comp.Title, ("docname", name));
        var description = Loc.GetString(entity.Comp.Description, ("docname", name));

        _metaDataSystem.SetEntityName(entity, title);
        _metaDataSystem.SetEntityDescription(entity, description);
        _objectives.SetIcon(entity, group.Sprite);
    }


    public bool GetAllOtherValidDocHoldObjectives(MindComponent mindComp, out List<(EntityUid, EntityUid)> validDocHoldTratorObjectives)
    {
        var docHoldTratorObjectives = _serverObjectivesSystem.GetAllOtherTratorsWithObjective(mindComp, "DocHoldObjective");
        validDocHoldTratorObjectives = new List<(EntityUid, EntityUid)>();
        foreach (var tratorAndObjective in docHoldTratorObjectives)
            if (TryComp<HoldDocumentObjectiveComponent>(tratorAndObjective.Item2, out var holdDocObjComp) && holdDocObjComp.IsAvailable)
                validDocHoldTratorObjectives.Add(tratorAndObjective);

        // No valid traitors!
        if (validDocHoldTratorObjectives.Count < 1)
            return false;

        return true;
    }
}
