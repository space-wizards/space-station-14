#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Atmos;
using Content.Server.Atmos.Reactions;
using Content.Server.GameObjects.Components.Atmos;
using Content.Shared.GameObjects.EntitySystems.Atmos;
using JetBrains.Annotations;
using Robust.Server.GameObjects.EntitySystems.TileLookup;
using Robust.Server.Interfaces.Timing;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Map;
using Robust.Shared.GameObjects.Systems;
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

        private GasReactionPrototype[] _gasReactions = Array.Empty<GasReactionPrototype>();

        private SpaceGridAtmosphereComponent _spaceAtmos = default!;
        private GridTileLookupSystem? _gridTileLookup = null;

        /// <summary>
        ///     List of gas reactions ordered by priority.
        /// </summary>
        public IEnumerable<GasReactionPrototype> GasReactions => _gasReactions!;

        public GridTileLookupSystem GridTileLookupSystem => _gridTileLookup ??= Get<GridTileLookupSystem>();

        public override void Initialize()
        {
            base.Initialize();

            _gasReactions = _protoMan.EnumeratePrototypes<GasReactionPrototype>().ToArray();
            Array.Sort(_gasReactions, (a, b) => b.Priority.CompareTo(a.Priority));

            _spaceAtmos = new SpaceGridAtmosphereComponent();
            _spaceAtmos.Initialize();
            IoCManager.InjectDependencies(_spaceAtmos);

            _mapManager.TileChanged += OnTileChanged;
        }

        public IGridAtmosphereComponent? GetGridAtmosphere(GridId gridId)
        {
            if (!gridId.IsValid())
            {
                return _spaceAtmos;
            }

            var grid = _mapManager.GetGrid(gridId);

            if (!EntityManager.TryGetEntity(grid.GridEntityId, out var gridEnt)) return _spaceAtmos;

            return gridEnt.TryGetComponent(out IGridAtmosphereComponent? atmos) ? atmos : _spaceAtmos;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var (mapGridComponent, gridAtmosphereComponent) in EntityManager.ComponentManager.EntityQuery<IMapGridComponent, IGridAtmosphereComponent>())
            {
                if (_pauseManager.IsGridPaused(mapGridComponent.GridIndex)) continue;

                gridAtmosphereComponent.Update(frameTime);
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
