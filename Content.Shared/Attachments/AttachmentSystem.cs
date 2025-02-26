using Content.Shared.Attachments.Components;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Timing;

namespace Content.Shared.Attachments;

public sealed class AttachmentSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly ISerializationManager _serializer = default!;
    [Dependency] private readonly EntityManager _entMan = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _netMan = default!;

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
        if (!TryComp(item, out AttachmentComponent? attachment)
            || uid.Comp.Components[args.Container.ID] is not {} compRegistry)
            return;
        Logger.Debug($"{_netMan.IsClient}...attach time");
        foreach (var (compName, compRegistryEntry) in compRegistry)
        {
            if (!_factory.TryGetRegistration(compName, out var componentRegistration))
                continue;

            var componentType = componentRegistration.Type;

            if (!HasComp(item, componentType) || (HasComp(uid, componentType) && !attachment.ForceComponents))
                continue;

            if (_entMan.TryGetComponent(uid, componentType, out var compExists))
                _entMan.RemoveComponent(uid, compExists);

            _entMan.AddComponent(uid, compRegistryEntry, overwrite: true);
            var comp = _entMan.GetComponent(uid, componentType);
            var itemComp = _entMan.GetComponent(item, componentType);
            uid.Comp.AddedComps.Add((item, comp.GetType()));
            // _serializer.CopyTo(itemComp, ref comp, notNullableOverride: true);

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
        Logger.Debug($"{_netMan.IsClient}...remove1 time");
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
