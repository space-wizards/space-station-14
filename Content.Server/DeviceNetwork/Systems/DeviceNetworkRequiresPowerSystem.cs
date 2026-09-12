using Content.Server.DeviceNetwork.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.DeviceNetwork.Events;

namespace Content.Server.DeviceNetwork.Systems;

public sealed partial class DeviceNetworkRequiresPowerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DeviceNetworkRequiresPowerComponent, BeforePacketSentEvent>(OnBeforePacketSent);
    }

    private void OnBeforePacketSent(Entity<DeviceNetworkRequiresPowerComponent> ent, ref BeforePacketSentEvent args)
    {
        if (!this.IsPowered(ent, EntityManager))
        {
            args.Cancelled = true;
        }
    }
}
