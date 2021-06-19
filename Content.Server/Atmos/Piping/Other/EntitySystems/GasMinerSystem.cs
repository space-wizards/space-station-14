using System.Diagnostics.CodeAnalysis;
using Content.Server.Atmos.Components;
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
            if (!CheckMinerOperation(args.Atmosphere, miner, out var tile) || !miner.Enabled || miner.SpawnGas <= Gas.Invalid || miner.SpawnAmount <= 0f)
                return;

            // Time to mine some gas.

            var merger = new GasMixture(1) { Temperature = miner.SpawnTemperature };
            merger.SetMoles(miner.SpawnGas, miner.SpawnAmount);

            tile.AssumeAir(merger);
        }

        private bool CheckMinerOperation(IGridAtmosphereComponent atmosphere, GasMinerComponent miner, [NotNullWhen(true)] out TileAtmosphere? tile)
        {
            tile = atmosphere.GetTile(miner.Owner.Transform.Coordinates)!;

            // Space.
            if (atmosphere.IsSpace(tile.GridIndices))
            {
                miner.Broken = true;
                return false;
            }

            // Airblocked location.
            if (tile.Air == null)
            {
                miner.Broken = true;
                return false;
            }

            // External pressure above threshold.
            if (!float.IsInfinity(miner.MaxExternalPressure) &&
                tile.Air.Pressure > miner.MaxExternalPressure - miner.SpawnAmount * miner.SpawnTemperature * Atmospherics.R / tile.Air.Volume)
            {
                miner.Broken = true;
                return false;
            }

            // External gas amount above threshold.
            if (!float.IsInfinity(miner.MaxExternalAmount) && tile.Air.TotalMoles > miner.MaxExternalAmount)
            {
                miner.Broken = true;
                return false;
            }

            miner.Broken = false;
            return true;
        }
    }
}
