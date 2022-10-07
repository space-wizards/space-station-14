using Content.Server.Atmos.Piping.Unary.EntitySystems;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.Atmos.Visuals;
using Content.Shared.Examine;
using Content.Shared.Destructible;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.NodeContainer;
using Robust.Shared.Timing;
using Robust.Server.GameObjects;
using Content.Server.NodeContainer.Nodes;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.Audio;
using Content.Server.Administration.Logs;
using Content.Shared.Database;



namespace Content.Server.Atmos.Portable
{
    public sealed class PortableScrubberSystem : EntitySystem
    {
        [Dependency] private readonly GasVentScrubberSystem _scrubberSystem = default!;
        [Dependency] private readonly GasCanisterSystem _canisterSystem = default!;
        [Dependency] private readonly GasPortableSystem _gasPortableSystem = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly TransformSystem _transformSystem = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly AmbientSoundSystem _ambientSound = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PortableScrubberComponent, AtmosDeviceUpdateEvent>(OnDeviceUpdated);
            SubscribeLocalEvent<PortableScrubberComponent, AnchorStateChangedEvent>(OnAnchorChanged);
            SubscribeLocalEvent<PortableScrubberComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<PortableScrubberComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<PortableScrubberComponent, DestructionEventArgs>(OnDestroyed);
            SubscribeLocalEvent<PortableScrubberComponent, GasAnalyzerScanEvent>(OnScrubberAnalyzed);
        }

        private void OnDeviceUpdated(EntityUid uid, PortableScrubberComponent component, AtmosDeviceUpdateEvent args)
        {
            if (!TryComp(uid, out AtmosDeviceComponent? device))
                return;

            var timeDelta = (float) (_gameTiming.CurTime - device.LastProcess).TotalSeconds;

            if (!component.Enabled)
                return;

            /// If we are on top of a connector port, empty into it.
            if (TryComp<NodeContainerComponent>(uid, out var nodeContainer)
                && nodeContainer.TryGetNode(component.PortName, out PortablePipeNode? portableNode)
                && portableNode.ConnectionsEnabled)
            {
                _atmosphereSystem.React(component.Air, portableNode);
                if (portableNode.NodeGroup is PipeNet {NodeCount: > 1} net)
                    _canisterSystem.MixContainerWithPipeNet(component.Air, net.Air);
            }

            if (component.Full)
            {
                UpdateAppearance(uid, true, false);
                return;
            }

            var xform = Transform(uid);

            if (xform.GridUid == null)
                return;

            var position = _transformSystem.GetGridOrMapTilePosition(uid, xform);

            var environment = _atmosphereSystem.GetTileMixture(xform.GridUid, xform.MapUid, position, true);

            var running = Scrub(timeDelta, component, environment);

            UpdateAppearance(uid, false, running);
            /// We scrub once to see if we can and set the animation
            if (!running)
                return;
            /// widenet
            foreach (var adjacent in _atmosphereSystem.GetAdjacentTileMixtures(xform.GridUid.Value, position, false, true))
            {
                Scrub(timeDelta, component, environment);
            }
        }

        /// <summary>
        /// If there is a port under us, let us connect with adjacent atmos pipes.
        /// </summary>
        private void OnAnchorChanged(EntityUid uid, PortableScrubberComponent component, ref AnchorStateChangedEvent args)
        {
            if (!TryComp(uid, out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode(component.PortName, out PipeNode? portableNode))
                return;

            portableNode.ConnectionsEnabled = (args.Anchored && _gasPortableSystem.FindGasPortIn(Transform(uid).GridUid, Transform(uid).Coordinates, out _));

            UpdateDrainingAppearance(uid, portableNode.ConnectionsEnabled);
        }
        private void OnPowerChanged(EntityUid uid, PortableScrubberComponent component, PowerChangedEvent args)
        {
            UpdateAppearance(uid, component.Full, args.Powered);
            component.Enabled = args.Powered;
        }

        /// <summary>
        /// Examining tells you how full it is as a %.
        /// </summary>
        private void OnExamined(EntityUid uid, PortableScrubberComponent component, ExaminedEvent args)
        {
            if (args.IsInDetailsRange)
            {
                var percentage = Math.Round(((component.Air.Pressure) / component.MaxPressure) * 100);
                args.PushMarkup(Loc.GetString("portable-scrubber-fill-level", ("percent", percentage)));
            }
        }

        /// <summary>
        /// When this is destroyed, we dump out all the gas inside.
        /// </summary>
        private void OnDestroyed(EntityUid uid, PortableScrubberComponent component, DestructionEventArgs args)
        {
            var environment = _atmosphereSystem.GetContainingMixture(uid, false, true);

            if (environment != null)
                _atmosphereSystem.Merge(environment, component.Air);

            _adminLogger.Add(LogType.CanisterPurged, LogImpact.Medium, $"Portable scrubber {ToPrettyString(uid):canister} purged its contents of {component.Air:gas} into the environment.");
            component.Air.Clear();
        }

        private bool Scrub(float timeDelta, PortableScrubberComponent scrubber, GasMixture? tile)
        {
            return _scrubberSystem.Scrub(timeDelta, scrubber.TransferRate, ScrubberPumpDirection.Scrubbing, scrubber.FilterGases, tile, scrubber.Air);
        }

        private void UpdateAppearance(EntityUid uid, bool isFull, bool isRunning)
        {
            if (!TryComp<AppearanceComponent>(uid, out var appearance))
                return;

            _ambientSound.SetAmbience(uid, isRunning);

            appearance.SetData(PortableScrubberVisuals.IsFull, isFull);
            appearance.SetData(PortableScrubberVisuals.IsRunning, isRunning);
        }

        private void UpdateDrainingAppearance(EntityUid uid, bool isDraining)
        {
            if (!TryComp<AppearanceComponent>(uid, out var appearance))
                return;

            appearance.SetData(PortableScrubberVisuals.IsDraining, isDraining);
        }

        /// <summary>
        /// Returns the gas mixture for the gas analyzer
        /// </summary>
        private void OnScrubberAnalyzed(EntityUid uid, PortableScrubberComponent component, GasAnalyzerScanEvent args)
        {
            var gasMixDict = new Dictionary<string, GasMixture?> { { Name(uid), component.Air } };
            // If it's connected to a port, include the port side
            if (TryComp(uid, out NodeContainerComponent? nodeContainer))
            {
                if(nodeContainer.TryGetNode(component.PortName, out PipeNode? port))
                    gasMixDict.Add(component.PortName, port.Air);
            }
            args.GasMixtures = gasMixDict;
        }
    }
}
