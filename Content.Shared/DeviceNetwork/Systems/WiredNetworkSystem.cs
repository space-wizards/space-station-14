using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Components.Networks;
using Content.Shared.DeviceNetwork.Events;

namespace Content.Shared.DeviceNetwork.Systems;

public sealed partial class WiredNetworkSystem : EntitySystem
{
    [Dependency] private DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private MetaDataSystem _meta = default!;

    [SubscribeLocalEvent]
    private void OnManagerInitialize(Entity<WiredNetworkComponent> ent, ref ComponentInit args)
    {
        _meta.AddFlag(ent, MetaDataFlags.ExtraTransformEvents);
    }

    [SubscribeLocalEvent]
    private void OnManagerInitialize(Entity<WiredNetworkManagerComponent> ent, ref DeviceNetworkManagerInitializeEvent args)
    {
        ent.Comp.GridId = Transform(args.Entity).GridUid;
    }

    [SubscribeLocalEvent]
    private void OnParentChanged(Entity<WiredNetworkComponent> ent, ref GridUidChangedEvent args)
    {
        _deviceNetwork.ReconnectDevice(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnAttemptConnect(Entity<WiredNetworkManagerComponent> ent, ref DeviceAttemptConnectEvent args)
    {
        if (Transform(args.Entity).GridUid == ent.Comp.GridId)
            args.Connected = true;
    }

    [SubscribeLocalEvent]
    private void OnBeforePacketSent(Entity<WiredNetworkComponent> ent, ref BeforePacketSentEvent args)
    {
        if (Transform(ent).GridUid != args.SenderTransform.GridUid)
            args.Cancelled = true;
    }

    //TODO Device Network, Things to do in a future PR:
    //Abstract out the connection between the apcExtensionCable and the apcPowerReceiver
    //Traverse the power cables using path traversal
    //Cache an optimized representation of the traversed path (Probably just cache Devices)
}
