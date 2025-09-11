using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Monitor.Systems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.Atmos.Piping.Binary.Systems;
using Content.Shared.Atmos.Piping.Components;
using Content.Shared.Audio;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Events;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Content.Shared.DeviceNetwork.Components;

namespace Content.Server.Atmos.Piping.Binary.EntitySystems
{
    [UsedImplicitly]
    public sealed class GasVolumePumpSystem : SharedGasVolumePumpSystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly SharedAmbientSoundSystem _ambientSoundSystem = default!;
        [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
        [Dependency] private readonly DeviceNetworkSystem _deviceNetwork = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasVolumePumpComponent, AtmosDeviceUpdateEvent>(OnVolumePumpUpdated);
            SubscribeLocalEvent<GasVolumePumpComponent, AtmosDeviceDisabledEvent>(OnVolumePumpLeaveAtmosphere);

            SubscribeLocalEvent<GasVolumePumpComponent, DeviceNetworkPacketEvent>(OnPacketRecv);
        }

        private void OnVolumePumpUpdated(EntityUid uid, GasVolumePumpComponent pump, ref AtmosDeviceUpdateEvent args)
        {
            if (!pump.Enabled ||
                (TryComp<ApcPowerReceiverComponent>(uid, out var power) && !power.Powered) ||
                !_nodeContainer.TryGetNodes(uid, pump.InletName, pump.OutletName, out PipeNode? inlet, out PipeNode? outlet))
            {
                _ambientSoundSystem.SetAmbience(uid, false);
                return;
            }

            var inputStartingPressure = inlet.Air.Pressure;
            var outputStartingPressure = outlet.Air.Pressure;

            var previouslyBlocked = pump.Blocked;
            pump.Blocked = false;

            // Pump mechanism won't do anything if the pressure is too high/too low unless you overclock it.
            if ((inputStartingPressure < pump.LowerThreshold) || (outputStartingPressure > pump.HigherThreshold) && !pump.Overclocked)
            {
                pump.Blocked = true;
            }

            // Overclocked pumps can only force gas a certain amount.
            if ((outputStartingPressure - inputStartingPressure > pump.OverclockThreshold) && pump.Overclocked)
            {
                pump.Blocked = true;
            }

            if (previouslyBlocked != pump.Blocked)
                UpdateAppearance(uid, pump);
            if (pump.Blocked)
                return;

            // We multiply the transfer rate in L/s by the seconds passed since the last process to get the liters.
            var removed = inlet.Air.RemoveVolume(pump.TransferRate * _atmosphereSystem.PumpSpeedup() * args.dt);

            // Some of the gas from the mixture leaks when overclocked.
            if (pump.Overclocked)
            {
                var tile = _atmosphereSystem.GetTileMixture(uid, excite: true);

                if (tile != null)
                {
                    var leaked = removed.RemoveRatio(pump.LeakRatio);
                    _atmosphereSystem.Merge(tile, leaked);
                }
            }

            pump.LastMolesTransferred = removed.TotalMoles;

            _atmosphereSystem.Merge(outlet.Air, removed);
            _ambientSoundSystem.SetAmbience(uid, removed.TotalMoles > 0f);
        }

        private void OnVolumePumpLeaveAtmosphere(EntityUid uid, GasVolumePumpComponent pump, ref AtmosDeviceDisabledEvent args)
        {
            pump.Enabled = false;
            Dirty(uid, pump);
            UpdateAppearance(uid, pump);
            _userInterfaceSystem.CloseUi(uid, GasVolumePumpUiKey.Key);
        }

        private void OnPacketRecv(EntityUid uid, GasVolumePumpComponent component, DeviceNetworkPacketEvent args)
        {
            if (!TryComp(uid, out DeviceNetworkComponent? netConn)
                || !args.Data.TryGetValue(DeviceNetworkConstants.Command, out var cmd))
            {
                return;
            }

            var payload = new NetworkPayload();

            switch (cmd)
            {
                case AtmosDeviceNetworkSystem.SyncData:
                    payload.Add(DeviceNetworkConstants.Command, AtmosDeviceNetworkSystem.SyncData);
                    payload.Add(AtmosDeviceNetworkSystem.SyncData, new GasVolumePumpData(component.LastMolesTransferred));

                    _deviceNetwork.QueuePacket(uid, args.SenderAddress, payload, device: netConn);
                    return;
            }
        }
    }
}
