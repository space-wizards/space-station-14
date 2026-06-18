using Content.Server.DeviceNetwork.Systems;
using Content.Server.Power.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;

namespace Content.Server.SensorMonitoring;

public sealed partial class BatterySensorSystem : EntitySystem
{
    [Dependency] private DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private SharedBatterySystem _battery = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BatterySensorComponent, DeviceNetworkPacketEvent>(PacketReceived);
    }

    private void PacketReceived(Entity<BatterySensorComponent> ent, ref DeviceNetworkPacketEvent args)
    {
        switch (args.Data)
        {
            case BatterySensorRequestPayload:
                var battery = Comp<BatteryComponent>(ent);
                var netBattery = Comp<PowerNetworkBatteryComponent>(ent);

                var payload = new BatterySensorSyncPayload
                {
                    Data = new BatterySensorData(
                        battery.CurrentCharge,
                        battery.MaxCharge,
                        netBattery.CurrentReceiving,
                        netBattery.MaxChargeRate,
                        netBattery.CurrentSupply,
                        netBattery.MaxSupply),
                };

                _deviceNetwork.QueuePacket(ent.Owner, args.SenderAddress, payload);
                break;
        }
    }
}
