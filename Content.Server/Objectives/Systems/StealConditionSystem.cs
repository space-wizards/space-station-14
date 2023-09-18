using Content.Server.Objectives.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Systems;

public sealed class StealConditionSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;

    private EntityQuery<ContainerManagerComponent> containerQuery;
    private EntityQuery<MetaDataComponent> metaQuery;

    public override void Initialize()
    {
        base.Initialize();

        containerQuery = GetEntityQuery<ContainerManagerComponent>();
        metaQuery = GetEntityQuery<MetaDataComponent>();

        SubscribeLocalEvent<StealConditionComponent, ObjectiveAssignedEvent>(OnAssigned);
        SubscribeLocalEvent<StealConditionComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);
        SubscribeLocalEvent<StealConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnAssigned(EntityUid uid, StealConditionComponent comp, ref ObjectiveAssignedEvent args)
    {
        // cancel if the item to steal doesn't exist
        args.Cancelled |= !_proto.HasIndex<EntityPrototype>(comp.Prototype);
    }

    private void OnAfterAssign(EntityUid uid, StealConditionComponent comp, ref ObjectiveAfterAssignEvent args)
    {
        var proto = _proto.Index<EntityPrototype>(comp.Prototype);
        var title = comp.OwnerText == null
            ? Loc.GetString("objective-condition-steal-title-no-owner", ("itemName", proto.Name))
            : Loc.GetString("objective-condition-steal-title", ("owner", Loc.GetString(comp.OwnerText)), ("itemName", proto.Name));
        var description = Loc.GetString("objective-condition-steal-description", ("itemName", proto.Name));

        _metaData.SetEntityName(uid, title, args.Meta);
        _metaData.SetEntityDescription(uid, description, args.Meta);
        _objectives.SetIcon(uid, new SpriteSpecifier.EntityPrototype(comp.Prototype), args.Objective);
    }

    private void OnGetProgress(EntityUid uid, StealConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetProgress(args.Mind, comp.Prototype);
    }

    private float GetProgress(MindComponent mind, string prototype)
    {
        // TODO make this a container system function
        // or: just iterate through transform children, instead of containers?

        if (!metaQuery.TryGetComponent(mind.OwnedEntity, out var meta))
            return 0;

        // who added this check bruh
        if (meta.EntityPrototype?.ID == prototype)
            return 1;

        if (!containerQuery.TryGetComponent(mind.OwnedEntity, out var currentManager))
            return 0;

        // recursively check each container for the item
        // checks inventory, bag, implants, etc.
        var stack = new Stack<ContainerManagerComponent>();
        do
        {
            foreach (var container in currentManager.Containers.Values)
            {
                foreach (var entity in container.ContainedEntities)
                {
                    // check if this is the item
                    if (metaQuery.GetComponent(entity).EntityPrototype?.ID == prototype)
                        return 1;

                    // if it is a container check its contents
                    if (containerQuery.TryGetComponent(entity, out var containerManager))
                        stack.Push(containerManager);
                }
            }
        } while (stack.TryPop(out currentManager));

        return 0;
    }
}
