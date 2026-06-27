using Content.Server.DeviceNetwork.Systems;
using Content.Server.Power.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.DeviceNetwork.Systems;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.SensorMonitoring;

namespace Content.Server.SensorMonitoring;

public sealed partial class BatterySensorSystem : DevicePayloadSystem<BatterySensorComponent>
{
    [Dependency] private DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private SharedBatterySystem _battery = default!;

    protected override void InitializeDevice()
    {
        base.InitializeDevice();
        SubscribePayload<BatterySensorSyncPayload>(OnSensorRequest);
    }

    private void OnSensorRequest(Entity<BatterySensorComponent> ent, ref BatterySensorSyncPayload payload, ref DeviceNetworkPacketData args)
    {
        var battery = Comp<BatteryComponent>(ent);
        var currentCharge = _battery.GetCharge((ent.Owner, battery));
        var netBattery = Comp<PowerNetworkBatteryComponent>(ent);

        var dataPayload = new BatterySensorDataPayload
        {
            Data = new BatterySensorData(
                currentCharge,
                battery.MaxCharge,
                netBattery.CurrentReceiving,
                netBattery.MaxChargeRate,
                netBattery.CurrentSupply,
                netBattery.MaxSupply),
        };

        _deviceNetwork.QueuePacket(ent.Owner, args.SenderAddress, dataPayload);
    }
}
