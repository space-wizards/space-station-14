using System.Threading;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Pathfinding;
using Content.Server.NPC.Systems;
using Content.Server.Transporters.Components;
using Content.Shared.Item;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Events;

namespace Content.Server.Transporters.Systems;

public sealed partial class TransporterSystem : EntitySystem
{
    [Dependency] private readonly ContainerSystem _containers = default!;
    [Dependency] private readonly HTNSystem _htn = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TransporterProviderComponent, StartCollideEvent>(OnProviderCollide);
        SubscribeLocalEvent<MarkedForTransportComponent, GettingPickedUpAttemptEvent>(OnMarkedItemPickup);
    }

    public void MarkForTransport(Entity<TransporterProviderComponent> provider, EntityUid item)
    {
        Logger.Debug($"Item {item.ToString()} ({ToPrettyString(item)}) has been marked for transport!");
        provider.Comp.CurrentUnclaimedItems.Add(item);
        EnsureComp(item, out MarkedForTransportComponent mark);
        mark.AssociatedProvider = provider.Owner;
    }

    public void UnmarkForTransport(EntityUid item)
    {
        Logger.Debug($"Item {item.ToString()} ({ToPrettyString(item)}) has been unmarked for transport!");
        if (!TryComp(item, out MarkedForTransportComponent? mark) ||
            mark.AssociatedProvider is null ||
            !TryComp(mark.AssociatedProvider, out TransporterProviderComponent? provider))
            return;

        provider.CurrentUnclaimedItems.Remove(item);
        RemComp(item, mark);
    }

    public void OnProviderCollide(EntityUid uid, TransporterProviderComponent component, ref StartCollideEvent args)
    {
        Logger.Debug("YOU SON OF A BITCH!");
        //if(!TryComp(uid, out ItemComponent? _))
        //    return;

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
        if (!TryComp(claimer, out TransporterComponent? transporterComponent)
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
