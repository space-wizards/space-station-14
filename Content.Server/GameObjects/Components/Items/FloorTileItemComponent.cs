using Content.Server.GameObjects.Components.Stack;
using Content.Server.Utility;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Maps;
using Content.Shared.Utility;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Items
{
    [RegisterComponent]
    public class FloorTileItemComponent : Component, IAfterInteract
    {
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;

        public override string Name => "FloorTile";
        private string _outputTile;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _outputTile, "output", "floor_steel");
        }

        public override void Initialize()
        {
            base.Initialize();
            Owner.EnsureComponent<StackComponent>();
        }

        public void AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (!eventArgs.InRangeUnobstructed(ignoreInsideBlocker: true, popup: true)) return;
            if (!Owner.TryGetComponent(out StackComponent stack)) return;

            var attacked = eventArgs.Target;
            var mapGrid = _mapManager.GetGrid(eventArgs.ClickLocation.GetGridId(Owner.EntityManager));
            var tile = mapGrid.GetTileRef(eventArgs.ClickLocation);
            var tileDef = (ContentTileDefinition)_tileDefinitionManager[tile.Tile.TypeId];

            if (tileDef.IsSubFloor && attacked == null && stack.Use(1))
            {
                var desiredTile = _tileDefinitionManager[_outputTile];
                mapGrid.SetTile(eventArgs.ClickLocation, new Tile(desiredTile.TileId));
                EntitySystem.Get<AudioSystem>().PlayAtCoords("/Audio/Items/genhit.ogg", eventArgs.ClickLocation);
            }


        }

    }
}
