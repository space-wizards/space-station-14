using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.Stack;
using Content.Shared.Audio;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Maps;
using Content.Shared.Sound;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.Tiles
{
    [RegisterComponent]
    public class FloorTileItemComponent : Component, IAfterInteract
    {
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;

        public override string Name => "FloorTile";
        [DataField("outputs", customTypeSerializer: typeof(PrototypeIdListSerializer<ContentTileDefinition>))]
        private List<string>? _outputTiles;

        [DataField("placeTileSound")] SoundSpecifier _placeTileSound = new SoundPathSpecifier("/Audio/Items/genhit.ogg");

        protected override void Initialize()
        {
            base.Initialize();
            Owner.EnsureComponent<StackComponent>();
        }

        private bool HasBaseTurf(ContentTileDefinition tileDef, string baseTurf)
        {
            foreach (var tileBaseTurf in tileDef.BaseTurfs)
            {
                if (baseTurf == tileBaseTurf)
                {
                    return true;
                }
            }

            return false;
        }

        private void PlaceAt(IMapGrid mapGrid, EntityCoordinates location, ushort tileId, float offset = 0)
        {
            mapGrid.SetTile(location.Offset(new Vector2(offset, offset)), new Tile(tileId));
            SoundSystem.Play(Filter.Pvs(location), _placeTileSound.GetSound(), location, AudioHelpers.WithVariation(0.125f));
        }

        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (!eventArgs.InRangeUnobstructed(ignoreInsideBlocker: true, popup: true))
                return true;

            if (!_entMan.TryGetComponent(Owner, out StackComponent? stack))
                return true;

            var mapManager = IoCManager.Resolve<IMapManager>();

            var location = eventArgs.ClickLocation.AlignWithClosestGridTile();
            var locationMap = location.ToMap(_entMan);
            if (locationMap.MapId == MapId.Nullspace)
                return true;
            mapManager.TryGetGrid(location.GetGridId(_entMan), out var mapGrid);

            if (_outputTiles == null)
                return true;

            foreach (var currentTile in _outputTiles)
            {
                var currentTileDefinition = (ContentTileDefinition) _tileDefinitionManager[currentTile];

                if (mapGrid != null)
                {
                    var tile = mapGrid.GetTileRef(location);
                    var baseTurf = (ContentTileDefinition) _tileDefinitionManager[tile.Tile.TypeId];

                    if (HasBaseTurf(currentTileDefinition, baseTurf.ID))
                    {
                        if (!EntitySystem.Get<StackSystem>().Use(Owner, 1, stack))
                            continue;

                        PlaceAt(mapGrid, location, currentTileDefinition.TileId);
                        break;
                    }
                }
                else if (HasBaseTurf(currentTileDefinition, "space"))
                {
                    mapGrid = mapManager.CreateGrid(locationMap.MapId);
                    mapGrid.WorldPosition = locationMap.Position;
                    location = new EntityCoordinates(mapGrid.GridEntityId, Vector2.Zero);
                    PlaceAt(mapGrid, location, _tileDefinitionManager[_outputTiles[0]].TileId, mapGrid.TileSize / 2f);
                    break;
                }
            }

            return true;
        }
    }
}
