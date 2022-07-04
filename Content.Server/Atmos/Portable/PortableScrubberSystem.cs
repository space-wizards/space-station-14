using Content.Server.Atmos.Piping.Unary.EntitySystems;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.EntitySystems;
using Robust.Shared.Timing;
using Robust.Server.GameObjects;

namespace Content.Server.Atmos.Portable
{
    public sealed class PortableScrubberSystem : EntitySystem
    {
        [Dependency] private readonly GasVentScrubberSystem _scrubberSystem = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly TransformSystem _transformSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PortableScrubberComponent, AtmosDeviceUpdateEvent>(OnDeviceUpdated);
        }

        private void OnDeviceUpdated(EntityUid uid, PortableScrubberComponent component, AtmosDeviceUpdateEvent args)
        {
            if (!TryComp(uid, out AtmosDeviceComponent? device))
                return;

            var timeDelta = (float) (_gameTiming.CurTime - device.LastProcess).TotalSeconds;

            if (!component.Enabled)
                return;

            var xform = Transform(uid);

            if (xform.GridUid == null)
                return;

            var position = _transformSystem.GetGridOrMapTilePosition(uid, xform);

            var environment = _atmosphereSystem.GetTileMixture(xform.GridUid, xform.MapUid, position, true);

            Scrub(timeDelta, component, environment);
        }

        private void Scrub(float timeDelta, PortableScrubberComponent scrubber, GasMixture? tile)
        {
            _scrubberSystem.Scrub(timeDelta, scrubber.TransferRate, ScrubberPumpDirection.Scrubbing, scrubber.FilterGases, tile, scrubber.Air);
        }
    }
}
