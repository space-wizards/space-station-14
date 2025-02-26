using Content.Shared.Attachments.Components;
using Content.Shared.Weapons.Melee;
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
        var item = args.Entity;
        if (!TryComp(item, out AttachmentComponent? attachment)
            || uid.Comp.Components[args.Container.ID] is not {} compRegistry)
            return;
        Logger.Debug($"{_netMan.IsClient}...attach {ToPrettyString(item)} time");
        foreach (var (compName, compRegistryEntry) in compRegistry)
        {
            if (!_factory.TryGetRegistration(compName, out var componentRegistration))
                continue;

            var componentType = componentRegistration.Type;

            if (!HasComp(item, componentType))
                continue;

            IComponent? comp;
            if (_timing.IsFirstTimePredicted)
            {
                if (!HasComp(uid, componentType) || attachment.ForceComponents)
                {
                    comp = _factory.GetComponent(compRegistryEntry);
                    _entMan.AddComponent(uid, comp, overwrite: attachment.ForceComponents);
                    uid.Comp.AddedComps.Add((item, comp.GetType()));
                }
            }
            else
            {
                if (_entMan.TryGetComponent(uid, componentType, out comp))
                    _serializer.CopyTo(compRegistryEntry.Component, ref comp, notNullableOverride: true);
            }

            if (_entMan.TryGetComponent(uid, componentType, out comp))
            {
                var itemComp = _entMan.GetComponent(item, componentType);
                _serializer.CopyTo(itemComp, ref comp, notNullableOverride: true);
            }
        }
        if (TryComp(uid, out MeleeWeaponComponent? m))
            Logger.Debug(m.WideAnimationRotation.ToString());
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
        Logger.Debug($"{_netMan.IsClient}...remove {ToPrettyString(item)} time");
        foreach (var compName in compRegistry.Keys)
        {
            if (!_factory.TryGetRegistration(compName, out var componentRegistration))
                continue;
            var componentType = componentRegistration.Type;

            if (uid.Comp.AddedComps.Find(comp => comp == (item, componentType)) is { } entry)
            {
                uid.Comp.AddedComps.RemoveAll(match => match == entry);
                _entMan.RemoveComponent(uid, componentType);
            }
        }
    }
}
