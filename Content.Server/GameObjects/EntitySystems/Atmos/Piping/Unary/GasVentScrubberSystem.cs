using System;
using Content.Server.Atmos;
using Content.Server.GameObjects.Components.Atmos.Piping;
using Content.Server.GameObjects.Components.Atmos.Piping.Unary;
using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.GameObjects.Components.Atmos;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.EntitySystems.Atmos.Piping.Unary
{
    [UsedImplicitly]
    public class GasVentScrubberSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasVentScrubberComponent, AtmosDeviceUpdateEvent>(OnVentScrubberUpdated);
            SubscribeLocalEvent<GasVentScrubberComponent, AtmosDeviceLeaveAtmosphereEvent>(OnVentScrubberLeaveAtmosphere);
        }

        private void OnVentScrubberUpdated(EntityUid uid, GasVentScrubberComponent scrubber, AtmosDeviceUpdateEvent args)
        {
            var appearance = scrubber.Owner.GetComponentOrNull<AppearanceComponent>();

            if (scrubber.Welded)
            {
                appearance?.SetData(ScrubberVisuals.State, ScrubberState.Welded);
                return;
            }

            appearance?.SetData(ScrubberVisuals.State, ScrubberState.Off);

            if (!scrubber.Enabled)
                return;

            if (!ComponentManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode(scrubber.OutletName, out PipeNode? outlet))
                return;

            var environment = args.Atmosphere.GetTile(scrubber.Owner.Transform.Coordinates)!;

            Scrub(scrubber, appearance, environment, outlet);

            if (!scrubber.WideNet) return;

            // Scrub adjacent tiles too.
            foreach (var adjacent in environment.AdjacentTiles)
            {
                // Pass null appearance, we don't need to set it there.
                Scrub(scrubber, null, adjacent, outlet);
            }
        }

        private void OnVentScrubberLeaveAtmosphere(EntityUid uid, GasVentScrubberComponent component, AtmosDeviceLeaveAtmosphereEvent args)
        {
            if (ComponentManager.TryGetComponent(uid, out AppearanceComponent? appearance))
            {
                appearance.SetData(ScrubberVisuals.State, ScrubberState.Off);
            }
        }

        private void Scrub(GasVentScrubberComponent scrubber, AppearanceComponent? appearance, TileAtmosphere? tile, PipeNode outlet)
        {
            // Cannot scrub if tile is null or air-blocked.
            if (tile?.Air == null)
                return;

            // Cannot scrub if pressure too high.
            if (outlet.Air.Pressure >= 50 * Atmospherics.OneAtmosphere)
                return;

            if (scrubber.PumpDirection == ScrubberPumpDirection.Scrubbing)
            {
                appearance?.SetData(ScrubberVisuals.State, scrubber.WideNet ? ScrubberState.WideScrub : ScrubberState.Scrub);
                var transferMoles = MathF.Min(1f, (scrubber.VolumeRate / tile.Air.Volume) * tile.Air.TotalMoles);

                // Take a gas sample.
                var removed = tile.Air.Remove(transferMoles);

                // Nothing left to remove from the tile.
                if (MathHelper.CloseTo(removed.TotalMoles, 0f))
                    return;

                removed.ScrubInto(outlet.Air, scrubber.FilterGases);

                // Remix the gases.
                tile.AssumeAir(removed);
            }
            else if (scrubber.PumpDirection == ScrubberPumpDirection.Siphoning)
            {
                appearance?.SetData(ScrubberVisuals.State, ScrubberState.Siphon);
                var transferMoles = tile.Air.TotalMoles * (scrubber.VolumeRate / tile.Air.Volume);

                var removed = tile.Air.Remove(transferMoles);

                outlet.Air.Merge(removed);
                tile.Invalidate();
            }
        }
    }
}
