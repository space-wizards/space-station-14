#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Atmos;
using Content.Server.Atmos.Reactions;
using Content.Server.GameObjects.Components.Atmos;
using Content.Server.Interfaces;
using Content.Shared.GameObjects.EntitySystems.Atmos;
using JetBrains.Annotations;
using Robust.Server.Interfaces.Timing;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Map;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class AtmosphereSystem : SharedAtmosphereSystem
    {
        [Dependency] private readonly IPrototypeManager _protoMan = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPauseManager _pauseManager = default!;
        [Dependency] private IEntityManager _entityManager = default!;

        private GasReactionPrototype[] _gasReactions = Array.Empty<GasReactionPrototype>();

        /// <summary>
        ///     List of gas reactions ordered by priority.
        /// </summary>
        public IEnumerable<GasReactionPrototype> GasReactions => _gasReactions!;

        /// <summary>
        ///     EventBus reference for gas reactions.
        /// </summary>
        public IEventBus EventBus => _entityManager.EventBus;

        public override void Initialize()
        {
            base.Initialize();

            _gasReactions = _protoMan.EnumeratePrototypes<GasReactionPrototype>().ToArray();
            Array.Sort(_gasReactions, (a, b) => b.Priority.CompareTo(a.Priority));

            _mapManager.TileChanged += OnTileChanged;
        }

        public IGridAtmosphereComponent? GetGridAtmosphere(GridId gridId)
        {
            // TODO Return space grid atmosphere for invalid grids or grids with no atmos
            var grid = _mapManager.GetGrid(gridId);

            if (!EntityManager.TryGetEntity(grid.GridEntityId, out var gridEnt)) return null;

            return gridEnt.TryGetComponent(out IGridAtmosphereComponent? atmos) ? atmos : null;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var (mapGridComponent, gridAtmosphereComponent) in EntityManager.ComponentManager.EntityQuery<IMapGridComponent, IGridAtmosphereComponent>())
            {
                if (_pauseManager.IsGridPaused(mapGridComponent.GridIndex)) continue;

                gridAtmosphereComponent.Update(frameTime);
            }

            // creadth: everything exposable by atmo should be updated as well
            foreach (var atmoExposableComponent in EntityManager.ComponentManager.EntityQuery<AtmoExposableComponent>())
            {
                // creadth TODO: should be updated by atmo component when entity lookups for tiles ready
                // cref: https://discordapp.com/channels/310555209753690112/560845886263918612/751436453208260670
                var ownerTransform = atmoExposableComponent.Owner.Transform;
                var atmo = GetGridAtmosphere(ownerTransform.GridID);
                var tile = atmo?.GetTile(ownerTransform.GridPosition);
                // creadth: we ignore airless for now: we have heat exchange of tile too small
                // for heat exchange. Heat radiation is too small so ignored as well for now
                if (tile?.Air == null) continue;
                atmoExposableComponent.Update(tile);
            }
        }

        private void OnTileChanged(object? sender, TileChangedEventArgs eventArgs)
        {
            // When a tile changes, we want to update it only if it's gone from
            // space -> not space or vice versa. So if the old tile is the
            // same as the new tile in terms of space-ness, ignore the change

            if (eventArgs.NewTile.Tile.IsEmpty == eventArgs.OldTile.IsEmpty)
            {
                return;
            }

            GetGridAtmosphere(eventArgs.NewTile.GridIndex)?.Invalidate(eventArgs.NewTile.GridIndices);
        }
    }
}
