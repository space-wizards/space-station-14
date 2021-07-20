#nullable disable warnings
#nullable enable annotations
using System.Runtime.CompilerServices;
using Content.Server.Atmos.Components;
using Content.Server.Interfaces;
using Content.Shared.Atmos;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos
{
    public class TileAtmosphere : IGasMixtureHolder
    {
        [ViewVariables]
        public int ArchivedCycle;

        [ViewVariables]
        public int CurrentCycle;

        [ViewVariables]
        public float Temperature { get; set; } = Atmospherics.T20C;

        [ViewVariables]
        public float TemperatureArchived { get; set; } = Atmospherics.T20C;

        [ViewVariables]
        public TileAtmosphere? PressureSpecificTarget { get; set; }

        [ViewVariables]
        public float PressureDifference { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public float HeatCapacity { get; set; } = 1f;

        [ViewVariables]
        public float ThermalConductivity { get; set; } = 0.05f;

        [ViewVariables]
        public bool Excited { get; set; }

        [ViewVariables]
        private readonly GridAtmosphereComponent _gridAtmosphereComponent;

        /// <summary>
        ///     Adjacent tiles in the same order as <see cref="AtmosDirection"/>. (NSEW)
        /// </summary>
        [ViewVariables]
        public readonly TileAtmosphere?[] AdjacentTiles = new TileAtmosphere[Atmospherics.Directions];

        [ViewVariables]
        public AtmosDirection AdjacentBits = AtmosDirection.Invalid;

        [ViewVariables]
        public MonstermosInfo MonstermosInfo;

        [ViewVariables]
        public Hotspot Hotspot;

        [ViewVariables]
        public AtmosDirection PressureDirection;

        [ViewVariables]
        public GridId GridIndex { get; }

        [ViewVariables]
        public TileRef? Tile => GridIndices.GetTileRef(GridIndex);

        [ViewVariables]
        public Vector2i GridIndices { get; }

        [ViewVariables]
        public ExcitedGroup? ExcitedGroup { get; set; }

        /// <summary>
        /// The air in this tile. If null, this tile is completely air-blocked.
        /// This can be immutable if the tile is spaced.
        /// </summary>
        [ViewVariables]
        public GasMixture? Air { get; set; }

        [ViewVariables]
        public float MaxFireTemperatureSustained { get; set; }

        [ViewVariables]
        public AtmosDirection BlockedAirflow { get; set; } = AtmosDirection.Invalid;

        public TileAtmosphere(GridAtmosphereComponent atmosphereComponent, GridId gridIndex, Vector2i gridIndices, GasMixture? mixture = null, bool immutable = false)
        {
            _gridAtmosphereComponent = atmosphereComponent;
            GridIndex = gridIndex;
            GridIndices = gridIndices;
            Air = mixture;

            if(immutable)
                Air?.MarkImmutable();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateVisuals()
        {
            if (Air == null) return;

            _gridAtmosphereComponent.GasTileOverlaySystem.Invalidate(GridIndex, GridIndices);
        }

        public void UpdateAdjacent()
        {
            for (var i = 0; i < Atmospherics.Directions; i++)
            {
                var direction = (AtmosDirection) (1 << i);

                var otherIndices = GridIndices.Offset(direction.ToDirection());

                var isSpace = _gridAtmosphereComponent.IsSpace(GridIndices);
                var adjacent = _gridAtmosphereComponent.GetTile(otherIndices, !isSpace);
                AdjacentTiles[direction.ToIndex()] = adjacent;
                adjacent?.UpdateAdjacent(direction.GetOpposite());

                if (adjacent != null && !BlockedAirflow.IsFlagSet(direction) && !_gridAtmosphereComponent.IsAirBlocked(adjacent.GridIndices, direction.GetOpposite()))
                {
                    AdjacentBits |= direction;
                }
            }
        }

        private void UpdateAdjacent(AtmosDirection direction)
        {
            AdjacentTiles[direction.ToIndex()] = _gridAtmosphereComponent.GetTile(GridIndices.Offset(direction.ToDirection()));

            if (!BlockedAirflow.IsFlagSet(direction) && !_gridAtmosphereComponent.IsAirBlocked(GridIndices.Offset(direction.ToDirection()), direction.GetOpposite()))
            {
                AdjacentBits |= direction;
            }
            else
            {
                AdjacentBits &= ~direction;
            }
        }
    }
}
