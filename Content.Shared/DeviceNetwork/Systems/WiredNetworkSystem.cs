using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;

namespace Content.Shared.DeviceNetwork.Systems;

public sealed partial class WiredNetworkSystem : EntitySystem
{
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
