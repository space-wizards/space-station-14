using System.Security.Principal;
using Content.Server.Transporters.Components;
using Content.Shared.Coordinates;
using Content.Shared.Item;
using Content.Shared.Pinpointer;
using Microsoft.CodeAnalysis.QuickInfo;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Events;

namespace Content.Server.Transporters.Systems;

public sealed partial class TransporterSystem : EntitySystem
{
    [Dependency] private readonly ContainerSystem _containers = default!;

    public readonly string ContainerKey = "item";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TransporterProviderComponent, StartCollideEvent>(OnProviderCollide); // fingerprints unrecognizable
        SubscribeLocalEvent<TransporterProviderComponent, EndCollideEvent>(OnProviderEndCollide); 

        SubscribeLocalEvent<TransporterMarkedComponent, GettingPickedUpAttemptEvent>(OnMarkedPickup);
    }

    public override void Update(float frameTime)
    {
        var transporters = EntityQuery<TransporterComponent>();
        foreach (var transporter in transporters)
        {

        }
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

    public bool TransporterPickup(EntityUid transporter, EntityUid item)
    {
        if (!HasComp<ItemComponent>(item))
            return false;

        var container = _containers.GetContainer(transporter, ContainerKey);

        if (!_containers.CanInsert(item, container))
            return false;



        return true;
    }

    public void OnProviderCollide(EntityUid uid, TransporterProviderComponent component, ref StartCollideEvent args)
    {

    }

    public void OnProviderEndCollide(EntityUid uid, TransporterProviderComponent component, ref EndCollideEvent args)
    {

    }

    public void OnMarkedPickup(EntityUid uid, TransporterMarkedComponent component,
        ref GettingPickedUpAttemptEvent args)
    {
        if (args.Cancelled)
            return;


    }
}
