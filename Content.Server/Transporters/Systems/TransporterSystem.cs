using Content.Server.Transporters.Components;
using Content.Shared.Coordinates;
using Content.Shared.Item;
using Robust.Server.Containers;
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
        SubscribeLocalEvent<TransporterProviderComponent, StartCollideEvent>(OnProviderCollide);
        SubscribeLocalEvent<MarkedForTransportComponent, GettingPickedUpAttemptEvent>(OnMarkedItemPickup);
    }

    public void MarkForTransport(Entity<TransporterProviderComponent> provider, EntityUid item)
    {
        if (HasComp<MarkedForTransportComponent>(item))
            return;

        Logger.Debug($"Item {item.ToString()} ({ToPrettyString(item)}) has been marked for transport!");
        provider.Comp.CurrentUnclaimedItems.Add(item);
        EnsureComp(item, out MarkedForTransportComponent mark);
        mark.AssociatedProvider = provider.Owner;
    }

    public void UnmarkForTransport(EntityUid item)
    {
        if (!TryComp(item, out MarkedForTransportComponent? mark) ||
            mark.AssociatedProvider is null ||
            !TryComp(mark.AssociatedProvider, out TransporterProviderComponent? provider))
            return;

        Logger.Debug($"Item {item.ToString()} ({ToPrettyString(item)}) has been unmarked for transport!");
        provider.CurrentUnclaimedItems.Remove(item);
        RemComp(item, mark);
    }

    public bool UnclaimedItemsExist()
    {
        foreach (var mark in EntityQuery<MarkedForTransportComponent>())
        {
            if (!mark.Claimed)
                return true;
        }
        return false;
    }

    public bool TransporterAttemptGrab(EntityUid transporter, EntityUid item)
    {
        if (!TryComp(transporter, out ContainerManagerComponent? containerMan))
            return false;

        var container = _containers.GetContainer(transporter, ContainerKey, containerMan);

        if (container.Count > 0)
            return false;
        return _containers.Insert(item, container);
    }

    public bool TransporterAttemptDrop(EntityUid transporter, EntityUid receiver)
    {
        if (!TryComp(transporter, out ContainerManagerComponent? containerMan))
            return false;

        var container = _containers.GetContainer(transporter, ContainerKey, containerMan);

        if (container.Count == 0)
            return false;

        var item = container.ContainedEntities[0]; // There's only one.
        if (_containers.Remove(item, container, destination: receiver.ToCoordinates()))
        {
            UnmarkForTransport(item);
            return true;
        }
        return false;
    }

    public void OnProviderCollide(EntityUid uid, TransporterProviderComponent component, ref StartCollideEvent args)
    {
        if(!HasComp<ItemComponent>(args.OtherEntity))
            return;

        MarkForTransport((uid, component), args.OtherEntity);
    }

    public void OnMarkedItemPickup(EntityUid uid, MarkedForTransportComponent component,
        ref GettingPickedUpAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        UnmarkForTransport(uid);
    }

    public bool ClaimItem(EntityUid claimer, EntityUid item)
    {
        if (!HasComp<TransporterComponent>(claimer)
            || !TryComp(item, out MarkedForTransportComponent? mark)
            || mark.Claimed)
            return false;

        mark.ClaimingTransporter = claimer;
        return true;
    }

    public EntityUid? GetTarget(Entity<TransporterComponent> uid)
    {
        return uid.Comp.Target;
    }
}
