using Content.Shared.Attachments.Components;
using Content.Shared.Fax;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Timing;

namespace Content.Shared.Attachments;

public abstract partial class SharedAttachmentSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly ISerializationManager _serializer = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AttachmentHolderComponent, EntInsertedIntoContainerMessage>(OnItemInsert);
        SubscribeLocalEvent<AttachmentHolderComponent, EntRemovedFromContainerMessage>(OnItemRemove);
        SubscribeLocalEvent<AttachmentHolderComponent, EntGotInsertedIntoContainerMessage>(OnInsertInto);
        SubscribeLocalEvent<AttachmentHolderComponent, EntGotRemovedFromContainerMessage>(OnRemoveFrom);
    }

    protected abstract void CopyComponentFields<T>(T source, ref T target, Type ComponentType, List<string> fields) where T : IComponent;

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

        var compRegistry = GetComponentsFromComp(uid.Comp, containerID);
        if (compRegistry is null)
            return;

        foreach (var (compName, compRegistryEntry) in compRegistry)
        {
            if (!_factory.TryGetRegistration(compName, out var componentRegistration))
                continue;

            var componentType = componentRegistration.Type;

            if (!HasComp(reference, componentType))
                continue;

            EntityManager.TryGetComponent(uid, componentType, out var comp);
            if (_timing.IsFirstTimePredicted || _netMan.IsServer)
            {
                if (comp is null || attachment.ForceComponents)
                {
                    comp = _factory.GetComponent(compRegistryEntry);
                    EntityManager.AddComponent(uid, comp, overwrite: attachment.ForceComponents);
                    uid.Comp.AddedComps.Add((reference, comp.GetType()));
                }
            }
            else
            {
                if (comp is {})
                    _serializer.CopyTo(compRegistryEntry.Component, ref comp, notNullableOverride: true);
            }

            if (comp is {} && _netMan.IsServer && uid.Comp.Fields?[compName] is {} fields)
            {
                CopyComponentFields(EntityManager.GetComponent(reference, componentType), ref comp, componentType, fields);
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
