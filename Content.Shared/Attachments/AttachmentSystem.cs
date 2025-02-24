using Content.Shared.Attachments.Components;
using Robust.Shared.Containers;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Timing;

namespace Content.Shared.Attachments;

public sealed class AttachmentSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly ISerializationManager _serializer = default!;
    [Dependency] private readonly EntityManager _entMan = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AttachableComponent, EntInsertedIntoContainerMessage>(OnInsert);
        SubscribeLocalEvent<AttachableComponent, EntRemovedFromContainerMessage>(OnRemove);
    }

    private void OnInsert(Entity<AttachableComponent> uid, ref EntInsertedIntoContainerMessage args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;
        var item = args.Entity;
        if (!HasComp<AttachmentComponent>(item)
            || uid.Comp.Components is not {} compList
            || compList[args.Container.ID] is not {} compRegistry)
            return;
        foreach (var (compName, compRegistryEntry) in compRegistry)
        {
            if (!_factory.TryGetRegistration(compName, out var componentRegistration))
                continue;
            var componentType = componentRegistration.Type;

            if (!HasComp(item, componentType) || HasComp(uid, componentType))
                continue;

            var comp = _serializer.CreateCopy(compRegistryEntry.Component, notNullableOverride: true);
            var itemComp = _entMan.GetComponent(item, componentType);
            uid.Comp.AddedComps.Add((item, comp.GetType()));
            foreach (var field in uid.Comp.Fields[compName])
            {

            }
            _serializer.CopyTo(itemComp, ref comp, notNullableOverride: true);
            AddComp(uid, comp);
            Dirty(uid, comp);
        }
    }

    private void OnRemove(Entity<AttachableComponent> uid, ref EntRemovedFromContainerMessage args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;
        var item = args.Entity;
        if (!HasComp<AttachmentComponent>(item)
            || uid.Comp.Components is not {} compList
            || compList[args.Container.ID] is not {} compRegistry
            || uid.Comp.AddedComps.Count == 0)
            return;
        foreach (var compName in compRegistry.Keys)
        {
            if (!_factory.TryGetRegistration(compName, out var componentRegistration))
                continue;
            var componentType = componentRegistration.Type;

            if (uid.Comp.AddedComps.Find(comp => comp == (item, componentType)) is { } entry)
            {
                uid.Comp.AddedComps.Remove(entry);
                _entMan.RemoveComponent(uid, componentType);
            }
        }
    }
}
