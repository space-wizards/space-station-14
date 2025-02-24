using System.Net.Mail;
using Content.Server.Attachments.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Server.Attachments;

public sealed class AttachmentSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly ISerializationManager _serializer = default!;
    [Dependency] private readonly EntityManager _entMan = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AttachableComponent, EntInsertedIntoContainerMessage>(OnInsert);
        SubscribeLocalEvent<AttachableComponent, EntRemovedFromContainerMessage>(OnRemove);
    }

    private void OnInsert(Entity<AttachableComponent> uid, ref EntInsertedIntoContainerMessage args)
    {
        var item = args.Entity;
        if (!HasComp<AttachmentComponent>(item)
            || uid.Comp.Components is not {} compList
            || compList[args.Container.ID] is not {} compRegistry)
            return;
        foreach (var (compName, compFields) in compRegistry)
        {
            var componentRegistration = _factory.GetRegistration(compName);
            var componentType = componentRegistration.Type;

            if (!HasComp(item, componentType) || HasComp(uid, componentType))
                continue;

            var comp = _factory.GetComponent(componentRegistration);
            var itemComp = _entMan.GetComponent(item, componentType);
            uid.Comp.AddedComps.Add((item, comp.GetType()));
            _serializer.CopyTo(itemComp, ref comp, notNullableOverride: true);
            AddComp(uid, comp);
        }
    }

    private void OnRemove(Entity<AttachableComponent> uid, ref EntRemovedFromContainerMessage args)
    {
        var item = args.Entity;
        if (!HasComp<AttachmentComponent>(item)
            || uid.Comp.Components is not {} compList
            || compList[args.Container.ID] is not {} compRegistry
            || uid.Comp.AddedComps.Count == 0)
            return;
        foreach (var compName in compRegistry.Keys)
        {
            var componentType = _factory.GetRegistration(compName).Type;

            if (uid.Comp.AddedComps.Find(comp => comp == (item, componentType)) is { } entry)
            {
                uid.Comp.AddedComps.Remove(entry);
                _entMan.RemoveComponent(uid, componentType);
            }
        }
    }
}
