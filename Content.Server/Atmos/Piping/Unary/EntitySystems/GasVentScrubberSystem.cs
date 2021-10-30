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
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Server.Atmos.Piping.Unary.EntitySystems
{
    [UsedImplicitly]
    public class GasVentScrubberSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;

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

            if (!scrubber.Enabled
            || !EntityManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer)
            || !nodeContainer.TryGetNode(scrubber.OutletName, out PipeNode? outlet))
            {
                appearance?.SetData(ScrubberVisuals.State, ScrubberState.Off);
                return;
            }

            var environment = _atmosphereSystem.GetTileMixture(scrubber.Owner.Transform.Coordinates, true);

            Scrub(_atmosphereSystem, scrubber, appearance, environment, outlet);

            if (!scrubber.WideNet) return;

            // Scrub adjacent tiles too.
            foreach (var adjacent in _atmosphereSystem.GetAdjacentTileMixtures(scrubber.Owner.Transform.Coordinates, false, true))
            {
                Scrub(_atmosphereSystem, scrubber, null, adjacent, outlet);
            }
        }

        private void OnVentScrubberLeaveAtmosphere(EntityUid uid, GasVentScrubberComponent component, AtmosDeviceDisabledEvent args)
        {
            if (EntityManager.TryGetComponent(uid, out AppearanceComponent? appearance))
            {
                appearance.SetData(ScrubberVisuals.State, ScrubberState.Off);
            }
        }

        private void Scrub(AtmosphereSystem atmosphereSystem, GasVentScrubberComponent scrubber, AppearanceComponent? appearance, GasMixture? tile, PipeNode outlet)
        {
            // Cannot scrub if tile is null or air-blocked.
            if (tile == null
                || outlet.Air.Pressure >= 50 * Atmospherics.OneAtmosphere) // Cannot scrub if pressure too high.
            {
                appearance?.SetData(ScrubberVisuals.State, ScrubberState.Off);
                return;
            }

            if (scrubber.PumpDirection == ScrubberPumpDirection.Scrubbing)
            {
                appearance?.SetData(ScrubberVisuals.State, scrubber.WideNet ? ScrubberState.WideScrub : ScrubberState.Scrub);
                var transferMoles = MathF.Min(1f, scrubber.VolumeRate / tile.Volume) * tile.TotalMoles;

                // Take a gas sample.
                var removed = tile.Remove(transferMoles);

                // Nothing left to remove from the tile.
                if (MathHelper.CloseToPercent(removed.TotalMoles, 0f))
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
