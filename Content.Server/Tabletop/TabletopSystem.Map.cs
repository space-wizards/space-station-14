using System.Numerics;
using Content.Shared.GameTicking;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Tabletop
{
    public sealed partial class TabletopSystem
    {
        /// <summary>
        ///     Separation between tabletops in the tabletop map.
        /// </summary>
        private const int TabletopSeparation = 100;

        /// <summary>
        ///     Map where all tabletops reside.
        /// </summary>
        public MapId TabletopMap { get; private set; } = MapId.Nullspace;

        /// <summary>
        ///     The number of tabletops created in the map.
        ///     Used for calculating the position of the next one.
        /// </summary>
        private int _tabletops = 0;

        /// <summary>
        ///     Despite the name, this method is only used to subscribe to events.
        /// </summary>
        private void InitializeMap()
        {
            SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        }

        /// <summary>
        ///     Gets the next available position for a tabletop, and increments the tabletop count.
        /// </summary>
        /// <returns></returns>
        private Vector2 GetNextTabletopPosition()
        {
            return UlamSpiral(_tabletops++) * TabletopSeparation;
        }

        /// <summary>
        ///     Ensures that the tabletop map exists. Creates it if it doesn't.
        /// </summary>
        private void EnsureTabletopMap()
        {
            if (TabletopMap != MapId.Nullspace && _map.MapExists(TabletopMap))
                return;

            var mapUid = _map.CreateMap(out var mapId);
            TabletopMap = mapId;
            _tabletops = 0;

            var mapComp = EntityManager.GetComponent<MapComponent>(mapUid);

            // Lighting is always disabled in tabletop world.
            mapComp.LightingEnabled = false;
            Dirty(mapUid, mapComp);
        }

        /// <summary>
        ///     Algorithm for mapping scalars to 2D positions in the same pattern as an Ulam Spiral.
        /// </summary>
        /// <param name="n">Scalar to map to a 2D position.</param>
        /// <returns>The mapped 2D position for the scalar.</returns>
        private Vector2i UlamSpiral(int n)
        {
            var k = (int)MathF.Ceiling(MathF.Sqrt(n) - 1) / 2;
            var t = 2 * k + 1;
            var m = (int)MathF.Pow(t, 2);
            t--;

            if (n >= m - t)
                return new Vector2i(k - (m - n), -k);

            m -= t;

            if (n >= m - t)
                return new Vector2i(-k, -k + (m - n));

            m -= t;

            if (n >= m - t)
                return new Vector2i(-k + (m - n), k);

            return new Vector2i(k, k - (m - n - t));
        }

        private void OnRoundRestart(RoundRestartCleanupEvent _)
        {
            if (TabletopMap == MapId.Nullspace || !_map.MapExists(TabletopMap))
                return;

            // This will usually *not* be the case, but better make sure.
            _map.DeleteMap(TabletopMap);

            // Reset tabletop count.
            _tabletops = 0;
        }
    }
}
