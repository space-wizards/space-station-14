using System;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping.Unary.Visuals;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Server.Atmos.Piping.Unary.EntitySystems
{
    [UsedImplicitly]
    public class GasVentScrubberSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasVentScrubberComponent, AtmosDeviceUpdateEvent>(OnVentScrubberUpdated);
            SubscribeLocalEvent<GasVentScrubberComponent, AtmosDeviceDisabledEvent>(OnVentScrubberLeaveAtmosphere);
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

            var atmosphereSystem = Get<AtmosphereSystem>();
            var environment = atmosphereSystem.GetTileMixture(scrubber.Owner.Transform.Coordinates, true);

            Scrub(atmosphereSystem, scrubber, appearance, environment, outlet);

            if (!scrubber.WideNet) return;

            // Scrub adjacent tiles too.
            foreach (var adjacent in atmosphereSystem.GetAdjacentTileMixtures(scrubber.Owner.Transform.Coordinates, false, true))
            {
                Scrub(atmosphereSystem, scrubber, null, adjacent, outlet);
            }
        }

        private void OnVentScrubberLeaveAtmosphere(EntityUid uid, GasVentScrubberComponent component, AtmosDeviceDisabledEvent args)
        {
            if (ComponentManager.TryGetComponent(uid, out AppearanceComponent? appearance))
            {
                appearance.SetData(ScrubberVisuals.State, ScrubberState.Off);
            }
        }

        private void Scrub(AtmosphereSystem atmosphereSystem, GasVentScrubberComponent scrubber, AppearanceComponent? appearance, GasMixture? tile, PipeNode outlet)
        {
            // Cannot scrub if tile is null or air-blocked.
            if (tile == null)
                return;

            // Cannot scrub if pressure too high.
            if (outlet.Air.Pressure >= 50 * Atmospherics.OneAtmosphere)
                return;

            if (scrubber.PumpDirection == ScrubberPumpDirection.Scrubbing)
            {
                appearance?.SetData(ScrubberVisuals.State, scrubber.WideNet ? ScrubberState.WideScrub : ScrubberState.Scrub);
                var transferMoles = MathF.Min(1f, (scrubber.VolumeRate / tile.Volume) * tile.TotalMoles);

                // Take a gas sample.
                var removed = tile.Remove(transferMoles);

                // Nothing left to remove from the tile.
                if (MathHelper.CloseTo(removed.TotalMoles, 0f))
                    return;

                atmosphereSystem.ScrubInto(removed, outlet.Air, scrubber.FilterGases);

                // Remix the gases.
                atmosphereSystem.Merge(tile, removed);
            }
            else if (scrubber.PumpDirection == ScrubberPumpDirection.Siphoning)
            {
                appearance?.SetData(ScrubberVisuals.State, ScrubberState.Siphon);
                var transferMoles = tile.TotalMoles * (scrubber.VolumeRate / tile.Volume);

                var removed = tile.Remove(transferMoles);

                outlet.AssumeAir(removed);
            }
        }
    }
}
