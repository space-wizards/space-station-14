using Content.Server.Actions;
using Content.Server.Buckle.Components;
using Content.Server.Inventory;
using Content.Server.Mind.Commands;
using Content.Shared.GameTicking;
using Robust.Shared.Map;

namespace Content.Server.Polymorph.Systems
{
    public sealed partial class PolymorphableSystem : EntitySystem
    {
        public MapId PausedMap { get; private set; }  = MapId.Nullspace;

        private void InitializeMap()
        {
            SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        }

        private void OnRoundRestart(RoundRestartCleanupEvent _)
        {
            if (PausedMap == MapId.Nullspace || !_mapManager.MapExists(PausedMap))
                return;

            _mapManager.DeleteMap(PausedMap);
        }

        private void EnsurePausesdMap()
        {
            if (PausedMap != MapId.Nullspace && _mapManager.MapExists(PausedMap))
                return;

            PausedMap = _mapManager.CreateMap();
            _mapManager.SetMapPaused(PausedMap, true);

            var mapComp = EntityManager.GetComponent<IMapComponent>(_mapManager.GetMapEntityId(PausedMap));

            mapComp.Dirty();
        }
    }
}
