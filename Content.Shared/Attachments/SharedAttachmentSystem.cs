using Content.Shared.Attachments.Components;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Timing;

namespace Content.Shared.Attachments;

public abstract partial class SharedAttachmentSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AttachmentHolderComponent, EntInsertedIntoContainerMessage>(OnItemInsert);
        SubscribeLocalEvent<AttachmentHolderComponent, EntRemovedFromContainerMessage>(OnItemRemove);
        SubscribeLocalEvent<AttachmentHolderComponent, EntGotInsertedIntoContainerMessage>(OnInsertInto);
        SubscribeLocalEvent<AttachmentHolderComponent, EntGotRemovedFromContainerMessage>(OnRemoveFrom);
        SubscribeLocalEvent<AttachmentHolderComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(Entity<AttachmentHolderComponent> uid, ref ComponentInit args)
    {
        if (!_netMan.IsServer || uid.Comp.Fields is null)
            return;
        foreach (var (compName, compFields) in uid.Comp.Fields)
        {
            if (!_factory.TryGetRegistration(compName, out var registration))
                continue;

            foreach (var field in compFields)
            {
                GetComponentFieldInfo(registration.Type, field);
            }
        }
    }

    protected abstract void CopyComponentFields<T>(T source, ref T target, Type ComponentType, List<string> fields) where T : IComponent;
    protected abstract object? GetComponentFieldInfo(Type type, string field);

    private void OnItemInsert(Entity<AttachmentHolderComponent> uid, ref EntInsertedIntoContainerMessage args)
        => AddComponentTo(uid, args.Entity, args.Container.ID);
    private void OnItemRemove(Entity<AttachmentHolderComponent> uid, ref EntRemovedFromContainerMessage args)
        => RemoveComponentFrom(uid, args.Entity, args.Container.ID);

    private void OnInsertInto(Entity<AttachmentHolderComponent> uid, ref EntGotInsertedIntoContainerMessage args)
        => AddComponentTo(uid, args.Container.Owner, args.Container.ID);
    private void OnRemoveFrom(Entity<AttachmentHolderComponent> uid, ref EntGotRemovedFromContainerMessage args)
        => RemoveComponentFrom(uid, args.Container.Owner, args.Container.ID);

    private ComponentRegistry? GetComponentsFromComp(AttachmentHolderComponent comp, string key)
    {
        if (comp.Components is { } compList
            && compList.TryGetValue(key, out var registry))
            return registry;
        if (comp.Prototypes is { } protoList
            && protoList.TryGetValue(key, out var protoId)
            && _proto.TryIndex(protoId, out var proto))
            return proto.Components;
        return null;
    }

    private void AddComponentTo(Entity<AttachmentHolderComponent> uid, EntityUid reference, string containerID)
    {
        if (!TryComp(reference, out AttachmentComponent? attachment))
            return;

        if (!_timing.ApplyingState && _netMan.IsClient)
            return;

        if (GetComponentsFromComp(uid.Comp, containerID) is not {} components)
            return;

        foreach (var (componentName, componentEntry) in components)
        {
            if (!_factory.TryGetRegistration(componentName, out var componentRegistration))
                continue;

            var componentType = componentRegistration.Type;

            if (!EntityManager.TryGetComponent(reference, componentType, out var referenceComp))
                continue;

            EntityManager.TryGetComponent(uid, componentType, out var comp);
            if (_netMan.IsServer)
            {
                if (comp is null || attachment.ForceComponents)
                {
                    comp = _factory.GetComponent(componentEntry);
                    EntityManager.AddComponent(uid, comp, overwrite: attachment.ForceComponents);
                    uid.Comp.AddedComps.Add((reference, comp.GetType()));
                }

                if (uid.Comp.CopyAllFields?.Contains(componentName) is true)
                {
                    CopyComp(reference, uid, referenceComp);
                }
                else if (uid.Comp.Fields?[componentName] is {} fields)
                {
                    CopyComponentFields(referenceComp, ref comp, componentType, fields);
                }
            }
            else
            {
                if (comp is {})
                    CopyComp(reference, uid, referenceComp);
            }
        }
    }

    private void RemoveComponentFrom(Entity<AttachmentHolderComponent> uid, EntityUid reference, string containerID)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!HasComp<AttachmentComponent>(reference)
            || uid.Comp.AddedComps.Count == 0)
            return;

        var compRegistry = GetComponentsFromComp(uid.Comp, containerID);
        if (compRegistry is null)
            return;
        foreach (var compName in compRegistry.Keys)
        {
            if (!_factory.TryGetRegistration(compName, out var componentRegistration))
                continue;
            var componentType = componentRegistration.Type;

            if (uid.Comp.AddedComps.Find(comp => comp == (reference, componentType)) is { } entry)
            {
                uid.Comp.AddedComps.RemoveAll(match => match == entry);
                EntityManager.RemoveComponent(uid, componentType);
            }
        }
    }
}
