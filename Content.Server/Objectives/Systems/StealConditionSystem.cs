using Content.Server.Objectives.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Systems;

public sealed class StealConditionSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private EntityQuery<ContainerManagerComponent> containerQuery;
    private EntityQuery<MetaDataComponent> metaQuery;

    public override void Initialize()
    {
        base.Initialize();

        containerQuery = GetEntityQuery<ContainerManagerComponent>();
        metaQuery = GetEntityQuery<MetaDataComponent>();

        SubscribeLocalEvent<StealConditionComponent, ObjectiveAssignedEvent>(OnAssigned);
        SubscribeLocalEvent<StealConditionComponent, ObjectiveGetInfoEvent>(OnGetInfo);
    }

    private void OnAssigned(EntityUid uid, StealConditionComponent comp, ref ObjectiveAssignedEvent args)
    {
        // cancel if the item to steal doesn't exist
        args.Cancelled |= !_proto.HasIndex<EntityPrototype>(comp.Prototype);
    }

    private void OnGetInfo(EntityUid uid, StealConditionComponent comp, ref ObjectiveGetInfoEvent args)
    {
        var proto = _proto.Index<EntityPrototype>(comp.Prototype);
        // nothing uses locale for it but might as well support it here incase
        var name = Loc.GetString(proto.Name);
        args.Info.Title = comp.OwnerText == null
            ? Loc.GetString("objective-condition-steal-title-no-owner", ("itemName", name))
            : Loc.GetString("objective-condition-steal-title", ("owner", Loc.GetString(comp.OwnerText)), ("itemName", name));
        args.Info.Icon = new SpriteSpecifier.EntityPrototype(comp.Prototype);
        args.Info.Progress = GetProgress(args.Mind, comp.Prototype);
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
