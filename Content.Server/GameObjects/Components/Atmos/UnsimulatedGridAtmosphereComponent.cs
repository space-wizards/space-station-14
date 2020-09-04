#nullable enable
using Content.Server.Atmos;
using Robust.Shared.Map;

namespace Content.Server.GameObjects.Components.Atmos
{
    public class UnsimulatedGridAtmosphereComponent : GridAtmosphereComponent, IGridAtmosphereComponent
    {
        public override string Name => "UnsimulatedGridAtmosphere";

        public override void FixVacuum(MapIndices indices) { }

        public override void AddActiveTile(TileAtmosphere? tile) { }

        public override void RemoveActiveTile(TileAtmosphere? tile) { }

        public override void AddHotspotTile(TileAtmosphere? tile) { }

        public override void RemoveHotspotTile(TileAtmosphere? tile) { }

        public override void AddSuperconductivityTile(TileAtmosphere? tile) { }

        public override void RemoveSuperconductivityTile(TileAtmosphere? tile) { }

        public override void AddHighPressureDelta(TileAtmosphere? tile) { }

        public override bool HasHighPressureDelta(TileAtmosphere tile)
        {
            return false;
        }

        public override void AddExcitedGroup(ExcitedGroup excitedGroup) { }

        public override void RemoveExcitedGroup(ExcitedGroup excitedGroup) { }

        public override void Update(float frameTime) { }

        public override bool ProcessTileEqualize(bool resumed = false)
        {
            return false;
        }

        public override bool ProcessActiveTiles(bool resumed = false)
        {
            return false;
        }

        public override bool ProcessExcitedGroups(bool resumed = false)
        {
            return false;
        }

        public override bool ProcessHighPressureDelta(bool resumed = false)
        {
            return false;
        }

        protected override bool ProcessHotspots(bool resumed = false)
        {
            return false;
        }

        protected override bool ProcessSuperconductivity(bool resumed = false)
        {
            return false;
        }

        protected override bool ProcessPipeNets(bool resumed = false)
        {
            return false;
        }

        protected override bool ProcessPipeNetDevices(bool resumed = false)
        {
            return false;
        }
    }
}
