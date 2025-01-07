using System.Diagnostics;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Server.PowerCell;
using Content.Server.Transporters.Components;
using Content.Shared.Item;
using Content.Shared.PowerCell.Components;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Events;

namespace Content.Server.Transporters.Systems;

public sealed partial class TransporterSystem : EntitySystem
{
    [Dependency] private readonly ContainerSystem _containers = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly NPCSystem _npc = default!;

    public readonly string ContainerKey = "item";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TransporterProviderComponent, StartCollideEvent>(OnProviderCollide); // fingerprints unrecognizable
        SubscribeLocalEvent<TransporterProviderComponent, EndCollideEvent>(OnProviderEndCollide);

        SubscribeLocalEvent<TransporterComponent, PowerCellChangedEvent>(OnPowerCellChanged);
    }

    public override void Update(float frameTime)
    {
        foreach (var transporter in GetTransporters())
        {
            if (Paused(transporter))
                continue;

            UpdateTransporter(transporter, frameTime);
        }
    }

    public IEnumerable<Entity<TransporterComponent>> GetTransporters()
    {
        var query = EntityQueryEnumerator<TransporterComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            yield return (uid, component);
        }
    }

    public void UpdateTransporter(Entity<TransporterComponent> uid, float frameTime)
    {
        _powerCell.TryUseCharge(uid, frameTime * uid.Comp.Wattage);
    }


    public void OnPowerCellChanged(EntityUid uid, TransporterComponent component, ref PowerCellChangedEvent args)
    {

    }

    public void AddItemToProvider(Entity<TransporterProviderComponent> provider, Entity<TransporterMarkedComponent> item)
    {
        if (item.Comp.AssociatedProvider is {} otherProvider)
        {
            if (TryComp(otherProvider, out TransporterProviderComponent? otherProviderComp))
                RemoveItemFromProvider((otherProvider, otherProviderComp), item);
        }

        provider.Comp.CurrentItems.Add(item);
        item.Comp.AssociatedProvider = provider;
    }

    public void RemoveItemFromProvider(Entity<TransporterProviderComponent> provider, Entity<TransporterMarkedComponent> item)
    {
        provider.Comp.CurrentItems.Remove(item);
        item.Comp.AssociatedProvider = null;
    }

    public bool UnmarkItem(EntityUid provider, EntityUid item)
    {
        if (TryComp(provider, out TransporterProviderComponent? providerComponent) &&
            TryComp(item, out TransporterMarkedComponent? markedComponent))
        {
            RemoveItemFromProvider((provider, providerComponent), (item, markedComponent));
            RemComp(item, markedComponent);
            return true;
        }

        return false;
    }

    public bool MarkItem(EntityUid provider, EntityUid item)
    {
        if (!TryComp(provider, out TransporterProviderComponent? providerComponent) ||
            HasComp<TransporterMarkedComponent>(item))
            return false;

        EnsureComp(item, out TransporterMarkedComponent markedComponent);
        AddItemToProvider((provider, providerComponent), (item, markedComponent));
        markedComponent.AssociatedProvider = provider;

        return true;
    }

    public bool TransporterAttemptPickup(EntityUid transporter, EntityUid item)
    {
        if (!HasComp<ItemComponent>(item))
            return false;

        var container = _containers.GetContainer(transporter, ContainerKey);

        if ((Transform(transporter).LocalPosition - Transform(item).LocalPosition).LengthSquared() < 2 * 2)
            return false;

        if (!_containers.CanInsert(item, container))
            return false;

        if (!TryComp(item, out TransporterMarkedComponent? marked))
            return false;

        return TransporterPickup(transporter, (item, marked), container);
    }

    public bool TransporterPickup(EntityUid transporter, Entity<TransporterMarkedComponent> item, BaseContainer? container = null)
    {
        container ??= _containers.GetContainer(transporter, ContainerKey);

        return _containers.Insert(item.Owner, container);
    }

    public void OnProviderCollide(EntityUid uid, TransporterProviderComponent component, ref StartCollideEvent args)
    {

    }

    public void OnProviderEndCollide(EntityUid uid, TransporterProviderComponent component, ref EndCollideEvent args)
    {

    }

    public void OnMarkedPickup(Entity<TransporterMarkedComponent> uid)
    {

    }
}
