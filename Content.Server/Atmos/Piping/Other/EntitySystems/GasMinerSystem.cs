using System.Diagnostics.CodeAnalysis;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Other.Components;
using Content.Shared.Atmos;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Atmos.Piping.Other.EntitySystems
{
    [UsedImplicitly]
    public class GasMinerSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasMinerComponent, AtmosDeviceUpdateEvent>(OnMinerUpdated);
        }

        private void OnMinerUpdated(EntityUid uid, GasMinerComponent miner, AtmosDeviceUpdateEvent args)
        {
            var atmosphereSystem = Get<AtmosphereSystem>();

            if (!CheckMinerOperation(atmosphereSystem, miner, out var environment) || !miner.Enabled || !miner.SpawnGas.HasValue || miner.SpawnAmount <= 0f)
                return;

            // Time to mine some gas.

            var merger = new GasMixture(1) { Temperature = miner.SpawnTemperature };
            merger.SetMoles(miner.SpawnGas.Value, miner.SpawnAmount);

            atmosphereSystem.Merge(environment, merger);
        }

        private bool CheckMinerOperation(AtmosphereSystem atmosphereSystem, GasMinerComponent miner, [NotNullWhen(true)] out GasMixture? environment)
        {
            environment = atmosphereSystem.GetTileMixture(miner.Owner.Transform.Coordinates, true);

            // Space.
            if (atmosphereSystem.IsTileSpace(miner.Owner.Transform.Coordinates))
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
