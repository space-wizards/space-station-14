using System.Diagnostics.CodeAnalysis;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Other.Components;
using Content.Shared.Atmos;
using JetBrains.Annotations;
using Robust.Server.GameObjects;

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

        private void OnMinerUpdated(Entity<GasMinerComponent> ent, ref AtmosDeviceUpdateEvent args)
        {
            var miner = ent.Comp;
            if (!CheckMinerOperation(ent, out var environment) || !miner.Enabled || !miner.SpawnGas.HasValue || miner.SpawnAmount <= 0f)
                return;

            // Time to mine some gas.

            var merger = new GasMixture(1) { Temperature = miner.SpawnTemperature };
            merger.SetMoles(miner.SpawnGas.Value, miner.SpawnAmount);

            _atmosphereSystem.Merge(environment, merger);
        }

        private bool CheckMinerOperation(Entity<GasMinerComponent> ent, [NotNullWhen(true)] out GasMixture? environment)
        {
            var (uid, miner) = ent;
            var transform = Transform(uid);
            environment = _atmosphereSystem.GetContainingMixture((uid, transform), true, true);

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
