using Content.Server.Objectives.Components;
using Content.Server.Objectives.Components.Targets;
using Robust.Shared.Prototypes;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;

namespace Content.Server.Objectives.Systems;

public sealed class HoldDocumentConditionSystem : EntitySystem
{
    [Dependency] private readonly StealConditionSystem _stealConditionSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HoldDocumentObjectiveComponent, ObjectiveItemGivenEvent>(OnObjectiveItemGivenEvent);
        SubscribeLocalEvent<HoldDocumentObjectiveComponent, ObjectiveAfterAssignEvent>(OnAfterAssign, after: new[] { typeof(StealConditionSystem) });
    }

    private void OnObjectiveItemGivenEvent(Entity<HoldDocumentObjectiveComponent> entity, ref ObjectiveItemGivenEvent args)
    {
        if (!TryComp<StealConditionComponent>(entity, out var stealComp))
            throw new Exception($"Missing StealConditionComponent for {entity}.");
        if (!TryComp<StealTargetComponent>(args.ItemUid, out var stealTargetComp) || stealTargetComp == null)
            throw new Exception($"Missing StealTargetComponent for {args.ItemUid}.");

        _stealConditionSystem.UpdateStealCondition((entity, stealComp), stealTargetComp.StealGroup);
    }

    private void OnAfterAssign(Entity<HoldDocumentObjectiveComponent> entity, ref ObjectiveAfterAssignEvent args)
    {
        if (!TryComp<StealConditionComponent>(entity, out var stealConditionComp))
            throw new Exception($"Missing StealConditionComponent for {entity}.");

        var group = _proto.Index(stealConditionComp.StealGroup);

        var title = Loc.GetString(entity.Comp.Title, ("docname", group.Name));
        var description = Loc.GetString(entity.Comp.Description, ("docname", group.Name));

        _metaDataSystem.SetEntityName(entity, title, args.Meta);
        _metaDataSystem.SetEntityDescription(entity, description, args.Meta);
        _objectives.SetIcon(entity, group.Sprite, args.Objective);
    }
}
