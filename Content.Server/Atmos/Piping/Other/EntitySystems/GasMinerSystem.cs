using System.Diagnostics.CodeAnalysis;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Other.Components;
using Content.Shared.Atmos;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Atmos.Piping.Other.EntitySystems
{
    [UsedImplicitly]
    public sealed class GasMinerSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly TransformSystem _transformSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasMinerComponent, AtmosDeviceUpdateEvent>(OnMinerUpdated);
        }

        private void OnMinerUpdated(EntityUid uid, GasMinerComponent miner, AtmosDeviceUpdateEvent args)
        {
            if (!CheckMinerOperation(miner, out var environment) || !miner.Enabled || !miner.SpawnGas.HasValue || miner.SpawnAmount <= 0f)
                return;

            // Time to mine some gas.

            var merger = new GasMixture(1) { Temperature = miner.SpawnTemperature };
            merger.SetMoles(miner.SpawnGas.Value, miner.SpawnAmount);

            _atmosphereSystem.Merge(environment, merger);
        }

        private bool CheckMinerOperation(GasMinerComponent miner, [NotNullWhen(true)] out GasMixture? environment)
        {
            var uid = miner.Owner;
            environment = _atmosphereSystem.GetContainingMixture(uid, true, true);

            var transform = Transform(uid);
            var position = _transformSystem.GetGridOrMapTilePosition(uid, transform);

            // Space.
            if (_atmosphereSystem.IsTileSpace(transform.GridUid, transform.MapUid, position))
            {
                miner.Broken = true;
                return false;
            }

            // Air-blocked location.
            if (environment == null)
            {
                miner.Broken = true;
                return false;
            }

            // External pressure above threshold.
            if (!float.IsInfinity(miner.MaxExternalPressure) &&
                environment.Pressure > miner.MaxExternalPressure - miner.SpawnAmount * miner.SpawnTemperature * Atmospherics.R / environment.Volume)
            {
                miner.Broken = true;
                return false;
            }

            // External gas amount above threshold.
            if (!float.IsInfinity(miner.MaxExternalAmount) && environment.TotalMoles > miner.MaxExternalAmount)
            {
                miner.Broken = true;
                return false;
            }

            miner.Broken = false;
            return true;
        }
    }
}
