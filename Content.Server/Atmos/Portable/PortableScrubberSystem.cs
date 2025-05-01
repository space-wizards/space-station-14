using Content.Server.Atmos.Piping.Unary.EntitySystems;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.Atmos.Visuals;
using Content.Shared.Examine;
using Content.Shared.Destructible;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.NodeContainer;
using Robust.Server.GameObjects;
using Content.Server.NodeContainer.Nodes;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.Audio;
using Content.Server.Administration.Logs;
using Content.Server.NodeContainer.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Database;
using Content.Shared.Power;

namespace Content.Server.Atmos.Portable
{
    public sealed class PortableScrubberSystem : EntitySystem
    {
        [Dependency] private readonly GasVentScrubberSystem _scrubberSystem = default!;
        [Dependency] private readonly GasCanisterSystem _canisterSystem = default!;
        [Dependency] private readonly GasPortableSystem _gasPortableSystem = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly TransformSystem _transformSystem = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly AmbientSoundSystem _ambientSound = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;

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

        private bool IsFull(PortableScrubberComponent component)
        {
            return component.Air.Pressure >= component.MaxPressure;
        }

        private void OnDeviceUpdated(EntityUid uid, PortableScrubberComponent component, ref AtmosDeviceUpdateEvent args)
        {
            var timeDelta = args.dt;

            if (!component.Enabled)
                return;

            // If we are on top of a connector port, empty into it.
            if (_nodeContainer.TryGetNode(uid, component.PortName, out PortablePipeNode? portableNode)
                && portableNode.ConnectionsEnabled)
            {
                _atmosphereSystem.React(component.Air, portableNode);
                if (portableNode.NodeGroup is PipeNet {NodeCount: > 1} net)
                    _canisterSystem.MixContainerWithPipeNet(component.Air, net.Air);
            }

            if (IsFull(component))
            {
                UpdateAppearance(uid, true, false);
                return;
            }

            if (args.Grid is not {} grid)
                return;

            var position = _transformSystem.GetGridTilePositionOrDefault(uid);
            var environment = _atmosphereSystem.GetTileMixture(grid, args.Map, position, true);

            var running = Scrub(timeDelta, component, environment);

            UpdateAppearance(uid, false, running);
            // We scrub once to see if we can and set the animation
            if (!running)
                return;

            // widenet
            var enumerator = _atmosphereSystem.GetAdjacentTileMixtures(grid, position, false, true);
            while (enumerator.MoveNext(out var adjacent))
            {
                Scrub(timeDelta, component, adjacent);
            }
        }

        /// <summary>
        /// If there is a port under us, let us connect with adjacent atmos pipes.
        /// </summary>
        private void OnAnchorChanged(EntityUid uid, PortableScrubberComponent component, ref AnchorStateChangedEvent args)
        {
            if (!_nodeContainer.TryGetNode(uid, component.PortName, out PipeNode? portableNode))
                return;

            portableNode.ConnectionsEnabled = (args.Anchored && _gasPortableSystem.FindGasPortIn(Transform(uid).GridUid, Transform(uid).Coordinates, out _));

            _appearance.SetData(uid, PortableScrubberVisuals.IsDraining, portableNode.ConnectionsEnabled);
        }

        private void OnPowerChanged(EntityUid uid, PortableScrubberComponent component, ref PowerChangedEvent args)
        {
            UpdateAppearance(uid, IsFull(component), args.Powered);
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

            _adminLogger.Add(LogType.CanisterPurged, LogImpact.Medium, $"Portable scrubber {ToPrettyString(uid):canister} purged its contents of {component.Air} into the environment.");
            component.Air.Clear();
        }

        private bool Scrub(float timeDelta, PortableScrubberComponent scrubber, GasMixture? tile)
        {
            return _scrubberSystem.Scrub(timeDelta, scrubber.TransferRate * _atmosphereSystem.PumpSpeedup(), ScrubberPumpDirection.Scrubbing, scrubber.FilterGases, tile, scrubber.Air);
        }

        private void UpdateAppearance(EntityUid uid, bool isFull, bool isRunning)
        {
            _ambientSound.SetAmbience(uid, isRunning);

            _appearance.SetData(uid, PortableScrubberVisuals.IsFull, isFull);
            _appearance.SetData(uid, PortableScrubberVisuals.IsRunning, isRunning);
        }

        /// <summary>
        /// Returns the gas mixture for the gas analyzer
        /// </summary>
        private void OnScrubberAnalyzed(EntityUid uid, PortableScrubberComponent component, GasAnalyzerScanEvent args)
        {
            args.GasMixtures ??= new List<(string, GasMixture?)>();
            args.GasMixtures.Add((Name(uid), component.Air));
        }
    }
}
