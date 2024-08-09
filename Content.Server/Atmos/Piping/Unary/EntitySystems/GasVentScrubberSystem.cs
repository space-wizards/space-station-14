using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Monitor.Components;
using Content.Server.Atmos.Monitor.Systems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping.Unary.Visuals;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.Audio;
using Content.Shared.DeviceNetwork;
using Content.Shared.Tools.Systems;
using JetBrains.Annotations;
using Robust.Server.GameObjects;

namespace Content.Server.Atmos.Piping.Unary.EntitySystems
{
    [UsedImplicitly]
    public sealed class GasVentScrubberSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly DeviceNetworkSystem _deviceNetSystem = default!;
        [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
        [Dependency] private readonly SharedAmbientSoundSystem _ambientSoundSystem = default!;
        [Dependency] private readonly TransformSystem _transformSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly WeldableSystem _weldable = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasVentScrubberComponent, AtmosDeviceUpdateEvent>(OnVentScrubberUpdated);
            SubscribeLocalEvent<GasVentScrubberComponent, AtmosDeviceEnabledEvent>(OnVentScrubberEnterAtmosphere);
            SubscribeLocalEvent<GasVentScrubberComponent, AtmosDeviceDisabledEvent>(OnVentScrubberLeaveAtmosphere);
            SubscribeLocalEvent<GasVentScrubberComponent, AtmosAlarmEvent>(OnAtmosAlarm);
            SubscribeLocalEvent<GasVentScrubberComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<GasVentScrubberComponent, DeviceNetworkPacketEvent>(OnPacketRecv);
            SubscribeLocalEvent<GasVentScrubberComponent, WeldableChangedEvent>(OnWeldChanged);
        }

        private void OnVentScrubberUpdated(EntityUid uid, GasVentScrubberComponent scrubber, ref AtmosDeviceUpdateEvent args)
        {
            if (_weldable.IsWelded(uid))
                return;

            var timeDelta = args.dt;

            if (!scrubber.Enabled || !_nodeContainer.TryGetNode(uid, scrubber.OutletName, out PipeNode? outlet))
                return;

            if (args.Grid is not {} grid)
                return;

            var position = _transformSystem.GetGridTilePositionOrDefault(uid);
            var environment = _atmosphereSystem.GetTileMixture(grid, args.Map, position, true);

            Scrub(timeDelta, scrubber, environment, outlet);

            if (!scrubber.WideNet)
                return;

            // Scrub adjacent tiles too.
            var enumerator = _atmosphereSystem.GetAdjacentTileMixtures(grid, position, false, true);
            while (enumerator.MoveNext(out var adjacent))
            {
                Scrub(timeDelta, scrubber, adjacent, outlet);
            }
        }

        private void OnVentScrubberLeaveAtmosphere(EntityUid uid, GasVentScrubberComponent component,
            AtmosDeviceDisabledEvent args) => UpdateState(uid, component);

        private void OnVentScrubberEnterAtmosphere(EntityUid uid, GasVentScrubberComponent component,
            AtmosDeviceEnabledEvent args) => UpdateState(uid, component);

        private void Scrub(float timeDelta, GasVentScrubberComponent scrubber, GasMixture? tile, PipeNode outlet)
        {
            Scrub(timeDelta, scrubber.TransferRate*_atmosphereSystem.PumpSpeedup(), scrubber.PumpDirection, scrubber.FilterGases, tile, outlet.Air);
        }

        /// <summary>
        /// True if we were able to scrub, false if we were not.
        /// </summary>
        public bool Scrub(float timeDelta, float transferRate, ScrubberPumpDirection mode, HashSet<Gas> filterGases, GasMixture? tile, GasMixture destination)
        {
            // Cannot scrub if tile is null or air-blocked.
            if (tile == null
                || destination.Pressure >= 50 * Atmospherics.OneAtmosphere) // Cannot scrub if pressure too high.
            {
                return false;
            }

            // Take a gas sample.
            var ratio = MathF.Min(1f, timeDelta * transferRate / tile.Volume);
            var removed = tile.RemoveRatio(ratio);

            // Nothing left to remove from the tile.
            if (MathHelper.CloseToPercent(removed.TotalMoles, 0f))
                return false;

            if (mode == ScrubberPumpDirection.Scrubbing)
            {
                _atmosphereSystem.ScrubInto(removed, destination, filterGases);

                // Remix the gases.
                _atmosphereSystem.Merge(tile, removed);
            }
            else if (mode == ScrubberPumpDirection.Siphoning)
            {
                _atmosphereSystem.Merge(destination, removed);
            }
            return true;
        }

        private void OnAtmosAlarm(EntityUid uid, GasVentScrubberComponent component, AtmosAlarmEvent args)
        {
            if (args.AlarmType == AtmosAlarmType.Danger)
            {
                component.Enabled = false;
            }
            else if (args.AlarmType == AtmosAlarmType.Normal)
            {
                component.Enabled = true;
            }

            UpdateState(uid, component);
        }

        private void OnPowerChanged(EntityUid uid, GasVentScrubberComponent component, ref PowerChangedEvent args)
        {
            component.Enabled = args.Powered;
            UpdateState(uid, component);
        }

        private void OnPacketRecv(EntityUid uid, GasVentScrubberComponent component, DeviceNetworkPacketEvent args)
        {
            if (!EntityManager.TryGetComponent(uid, out DeviceNetworkComponent? netConn)
                || !args.Data.TryGetValue(DeviceNetworkConstants.Command, out var cmd))
                return;

            var payload = new NetworkPayload();

            switch (cmd)
            {
                case AtmosDeviceNetworkSystem.SyncData:
                    payload.Add(DeviceNetworkConstants.Command, AtmosDeviceNetworkSystem.SyncData);
                    payload.Add(AtmosDeviceNetworkSystem.SyncData, component.ToAirAlarmData());

                    _deviceNetSystem.QueuePacket(uid, args.SenderAddress, payload, device: netConn);

                    return;
                case DeviceNetworkConstants.CmdSetState:
                    if (!args.Data.TryGetValue(DeviceNetworkConstants.CmdSetState, out GasVentScrubberData? setData))
                        break;

                    component.FromAirAlarmData(setData);
                    UpdateState(uid, component);

                    return;
            }
        }

        /// <summary>
        ///     Updates a scrubber's appearance and ambience state.
        /// </summary>
        private void UpdateState(EntityUid uid, GasVentScrubberComponent scrubber,
            AppearanceComponent? appearance = null)
        {
            if (!Resolve(uid, ref appearance, false))
                return;

            _ambientSoundSystem.SetAmbience(uid, true);
            if (_weldable.IsWelded(uid))
            {
                _ambientSoundSystem.SetAmbience(uid, false);
                _appearance.SetData(uid, ScrubberVisuals.State, ScrubberState.Welded, appearance);
            }
            else if (!scrubber.Enabled)
            {
                _ambientSoundSystem.SetAmbience(uid, false);
                _appearance.SetData(uid, ScrubberVisuals.State, ScrubberState.Off, appearance);
            }
            else if (scrubber.PumpDirection == ScrubberPumpDirection.Scrubbing)
            {
                _appearance.SetData(uid, ScrubberVisuals.State, scrubber.WideNet ? ScrubberState.WideScrub : ScrubberState.Scrub, appearance);
            }
            else if (scrubber.PumpDirection == ScrubberPumpDirection.Siphoning)
            {
                _appearance.SetData(uid, ScrubberVisuals.State, ScrubberState.Siphon, appearance);
            }
        }

        private void OnWeldChanged(EntityUid uid, GasVentScrubberComponent component, ref WeldableChangedEvent args)
        {
            UpdateState(uid, component);
        }
    }
}
