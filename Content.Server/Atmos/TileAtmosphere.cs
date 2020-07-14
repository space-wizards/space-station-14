using Robust.Shared.Map;

namespace Content.Server.Atmos
{
    public class TileAtmosphere
    {
        public MapId MapIndex { get; }
        public GridId GridIndex { get; }
        public MapIndices GridIndices { get; }
        public Tile Tile { get; }
        public ZoneAtmosphere Zone { get; internal set; }
        public GasMixture Air { get; }

        public TileAtmosphere(TileRef tile, float volume=0f)
        {
            MapIndex = tile.MapIndex;
            GridIndex = tile.GridIndex;
            GridIndices = tile.GridIndices;
            Tile = tile.Tile;

            // TODO ATMOS Load default gases from tile here or something
            Air = new GasMixture(volume);
        }
    }
}
