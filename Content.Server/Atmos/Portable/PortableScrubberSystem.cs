using Content.Server.Atmos.Piping.Unary.EntitySystems;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.Atmos.Visuals;
using Content.Shared.Examine;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.NodeContainer;
using Robust.Shared.Timing;
using Robust.Server.GameObjects;
using Content.Server.NodeContainer.Nodes;
using Content.Server.NodeContainer.NodeGroups;


namespace Content.Server.Atmos.Portable
{
    public sealed class PortableScrubberSystem : EntitySystem
    {
        [Dependency] private readonly GasVentScrubberSystem _scrubberSystem = default!;

        [Dependency] private readonly GasPortableSystem _gasPortableSystem = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly TransformSystem _transformSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PortableScrubberComponent, AtmosDeviceUpdateEvent>(OnDeviceUpdated);
            SubscribeLocalEvent<PortableScrubberComponent, AnchorStateChangedEvent>(OnAnchorChanged);
            SubscribeLocalEvent<PortableScrubberComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<PortableScrubberComponent, ExaminedEvent>(OnExamined);
        }

        private void OnDeviceUpdated(EntityUid uid, PortableScrubberComponent component, AtmosDeviceUpdateEvent args)
        {
            if (!TryComp(uid, out AtmosDeviceComponent? device))
                return;

            var timeDelta = (float) (_gameTiming.CurTime - device.LastProcess).TotalSeconds;

            if (!component.Enabled)
                return;

            if (TryComp<NodeContainerComponent>(uid, out var nodeContainer)
                && nodeContainer.TryGetNode(component.PortName, out PortablePipeNode? portableNode)
                && portableNode.ConnectionsEnabled)
            {
                HandleEmptying(component, portableNode);
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

            UpdateAppearance(uid, false, Scrub(timeDelta, component, environment));
        }

        private void OnAnchorChanged(EntityUid uid, PortableScrubberComponent component, ref AnchorStateChangedEvent args)
        {
            if (!TryComp(uid, out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode(component.PortName, out PipeNode? portableNode))
                return;

            Logger.Error("Setting portableNode connections enabled to " + (args.Anchored && _gasPortableSystem.FindGasPortIn(Transform(uid).GridUid, Transform(uid).Coordinates, out _)));
            portableNode.ConnectionsEnabled = (args.Anchored && _gasPortableSystem.FindGasPortIn(Transform(uid).GridUid, Transform(uid).Coordinates, out _));
        }

        private void OnPowerChanged(EntityUid uid, PortableScrubberComponent component, PowerChangedEvent args)
        {
            UpdateAppearance(uid,component.Full, args.Powered);
            component.Enabled = args.Powered;
        }

        private void OnExamined(EntityUid uid, PortableScrubberComponent component, ExaminedEvent args)
        {
            if (args.IsInDetailsRange)
            {
                var percentage = Math.Round(((component.Air.Pressure) / component.MaxPressure) * 100);
                args.PushMarkup("It's at about " + percentage + "% of its maximum internal pressure.");
            }
        }

        private bool Scrub(float timeDelta, PortableScrubberComponent scrubber, GasMixture? tile)
        {
            return _scrubberSystem.Scrub(timeDelta, scrubber.TransferRate, ScrubberPumpDirection.Scrubbing, scrubber.FilterGases, tile, scrubber.Air);
        }

        private void HandleEmptying(PortableScrubberComponent scrubber, PortablePipeNode node)
        {
            Logger.Error("Handling emptying...");
            _atmosphereSystem.React(scrubber.Air, node);

            if (node.NodeGroup is PipeNet {NodeCount: > 1} net)
            {
                var buffer = new GasMixture(net.Air.Volume + scrubber.Air.Volume);

                _atmosphereSystem.Merge(buffer, net.Air);
                _atmosphereSystem.Merge(buffer, scrubber.Air);

                net.Air.Clear();
                _atmosphereSystem.Merge(net.Air, buffer);
                net.Air.Multiply(net.Air.Volume / buffer.Volume);

                scrubber.Air.Clear();
                _atmosphereSystem.Merge(scrubber.Air, buffer);
                scrubber.Air.Multiply(scrubber.Air.Volume / buffer.Volume);
            }
        }

        private void UpdateAppearance(EntityUid uid, bool isFull, bool isRunning)
        {
            if (!TryComp<AppearanceComponent>(uid, out var appearance))
                return;

            appearance.SetData(PortableScrubberVisuals.IsFull, isFull);
            appearance.SetData(PortableScrubberVisuals.IsRunning, isRunning);
        }
    }
}
