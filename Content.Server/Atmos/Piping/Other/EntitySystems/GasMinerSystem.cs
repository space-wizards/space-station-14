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

            // SpawnAmount is declared in mol/s so to get the amount of gas we hope to mine, we have to multiply this by
            // how long we have been waiting to spawn it and further cap the number according to the miner's state.
            var toSpawn = CapSpawnAmount(ent, miner.SpawnAmount * args.dt, out var environment);
            if (toSpawn <= 0f || environment == null || !miner.Enabled || !miner.SpawnGas.HasValue)
                return;

            // Time to mine some gas.

            var merger = new GasMixture(1) { Temperature = miner.SpawnTemperature };
            merger.SetMoles(miner.SpawnGas.Value, toSpawn);

            _atmosphereSystem.Merge(environment, merger);
        }

        private float CapSpawnAmount(Entity<GasMinerComponent> ent, float toSpawnTarget, out GasMixture? environment)
        {
            var (uid, miner) = ent;
            var transform = Transform(uid);
            environment = _atmosphereSystem.GetContainingMixture((uid, transform), true, true);

            var position = _transformSystem.GetGridOrMapTilePosition(uid, transform);

            // Space.
            if (_atmosphereSystem.IsTileSpace(transform.GridUid, transform.MapUid, position))
            {
                miner.Broken = true;
                return 0f;
            }

            // Air-blocked location.
            if (environment == null)
            {
                miner.Broken = true;
                return 0f;
            }

            // How many moles could we theoretically spawn. Cap by pressure and amount.
            var allowableMoles = Math.Min(
                (miner.MaxExternalPressure - environment.Pressure) * environment.Volume / (miner.SpawnTemperature * Atmospherics.R),
                miner.MaxExternalAmount - environment.TotalMoles);

            var toSpawnReal = Math.Clamp(allowableMoles, 0f, toSpawnTarget);

            if (toSpawnReal < Atmospherics.GasMinMoles) {
                miner.Broken = true;
                return 0f;
            }

            miner.Broken = false;
            return toSpawnReal;
        }
    }
}
