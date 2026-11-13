using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Components.Networks;
using Content.Shared.DeviceNetwork.Events;

namespace Content.Shared.DeviceNetwork.Systems;

public sealed partial class WiredNetworkSystem : EntitySystem
{
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private DeviceNetworkSystem _deviceSystem = default!;

    [Dependency] private EntityQuery<WiredNetworkComponent> _wiredQuery;

    [SubscribeLocalEvent]
    private void OnWiredInit(Entity<WiredNetworkComponent> ent, ref MapInitEvent args)
    {
        _metaData.AddFlag(ent.Owner, MetaDataFlags.ExtraTransformEvents);
    }

    [SubscribeLocalEvent]
    private void OnConnectNetwork(Entity<WiredNetworkManagerComponent> ent, ref DeviceAttemptConnectEvent args)
    {
        if (_deviceSystem.CheckNetworkIgnore(args, _wiredQuery))
            return;

        args.Connected = Transform(args.Entity.Owner).GridUid == ent.Comp.GridUid;
    }

    [SubscribeLocalEvent]
    private void OnManagerInit(Entity<WiredNetworkManagerComponent> ent, ref DeviceNetworkManagerInitializeEvent args)
    {
        var grid = Transform(args.Entity.Owner).GridUid;
        ent.Comp.GridUid = grid;
        if (grid != null)
            EnsureComp<DeviceManagerRelatedComponent>(grid.Value).Managers.Add(ent.Owner);
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnManagerInit(Entity<WiredNetworkManagerComponent> ent, ref DeviceManagerRelatedShutdownEvent args)
    {
        if (ent.Comp.GridUid == args.Related)
            QueueDel(ent);
    }

    [SubscribeLocalEvent]
    private void OnManagerShutdown(Entity<WiredNetworkManagerComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent.Comp.GridUid)
            || !TryComp(ent.Comp.GridUid, out DeviceManagerRelatedComponent? related))
            return;

        related.Managers.Remove(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnGridChanged(Entity<WiredNetworkComponent> ent, ref GridUidChangedEvent args)
    {
        if (MetaData(ent.Owner).EntityLifeStage < EntityLifeStage.MapInitialized
            || args.OldGrid == args.NewGrid)
            return;

        _deviceSystem.ReconnectDevice(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnBeforePacketSent(Entity<WiredNetworkComponent> ent, ref BeforePacketSentEvent args)
    {
        if (Transform(ent).GridUid != args.SenderTransform.GridUid)
            args.Cancelled = true;
    }

    // Welcome to SS14 code comment museum.
    // Below you can see an ancient relic from the past when the very foundations of SS14 were developed -
    // this is a "to do" comment about remaking device packets that travel through wires.
    // This didn't end up happening, and it's unlikely to happen any time in the future,
    // since this change alone would require breaking mapping changes and refactoring other ancient systems.
    // Let this comment be preserved for future generations of coders so that maybe one day
    // someone brave enough will implement this idea.

    //TODO Device Network, Things to do in a future PR:
    //Abstract out the connection between the apcExtensionCable and the apcPowerReceiver
    //Traverse the power cables using path traversal
    //Cache an optimized representation of the traversed path (Probably just cache Devices)
}
