using System.Diagnostics.CodeAnalysis;
using Content.Server.Atmos;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.GameObjects;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos.Piping.Other
{
    [RegisterComponent]
    public class GasMinerComponent : Component, IAtmosProcess
    {
        public override string Name => "GasMiner";

        private bool _enabled = true;

        private bool _broken = false;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("maxExternalAmount")]
        public float MaxExternalAmount = float.PositiveInfinity;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("maxExternalPressure")]
        public float MaxExternalPressure = 6500f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("spawnGas")]
        public Gas SpawnGas { get; set; } = Gas.Invalid;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("spawnTemperature")]
        public float SpawnTemperature { get; set; } = Atmospherics.T20C;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("spawnAmount")]
        public float SpawnAmount { get; set; } = Atmospherics.MolesCellStandard * 20f;

        private bool CheckOperation([NotNullWhen(true)] out TileAtmosphere? tile)
        {
            var atmosphere = EntitySystem.Get<AtmosphereSystem>().GetGridAtmosphere(Owner.Transform.Coordinates);
            tile = atmosphere.GetTile(Owner.Transform.Coordinates)!;

            // Space.
            if (atmosphere.IsSpace(tile.GridIndices))
            {
                _broken = true;
                return false;
            }

            // Airblocked location.
            if (tile.Air == null)
            {
                _broken = true;
                return false;
            }

            // External pressure above threshold.
            if (!float.IsInfinity(MaxExternalPressure) &&
                tile.Air.Pressure > MaxExternalPressure - SpawnAmount * SpawnTemperature * Atmospherics.R / tile.Air.Volume)
            {
                _broken = true;
                return false;
            }

            // External gas amount above threshold.
            if (!float.IsInfinity(MaxExternalAmount) && tile.Air.TotalMoles > MaxExternalAmount)
            {
                _broken = true;
                return false;
            }

            _broken = false;
            return true;
        }

        public void ProcessAtmos(float time, IGridAtmosphereComponent atmosphere)
        {
            if (!CheckOperation(out var tile) || !_enabled || SpawnGas <= Gas.Invalid || SpawnAmount <= 0f)
                return;

            // Time to mine some gas.

            var merger = new GasMixture(1) { Temperature = SpawnTemperature };
            merger.SetMoles(SpawnGas, SpawnAmount);

            tile.AssumeAir(merger);
        }
    }
}
