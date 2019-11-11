using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Maps;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.Components.Items
{
    [RegisterComponent]
    public class FloorTileItemComponent : Component, IAfterAttack, IExamine
    {
#pragma warning disable 649
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
        [Dependency] private readonly IMapManager _mapManager;
#pragma warning restore 649

        public override string Name => "FloorTile";
        public int CurrentStackCount = 1;
        public string _outputTile;
        private int _maxStackCount;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _outputTile, "output", "floor_steel");
            serializer.DataField(ref _maxStackCount, "max_stack_size", 8);
        }



        public void AfterAttack(AfterAttackEventArgs eventArgs)
        {
            //Check for clicking on another tile to add to the stack.
            var attacked = eventArgs.Attacked;
            if(attacked != null && attacked.TryGetComponent<FloorTileItemComponent>(out var floorTileComp))
            {
                if (floorTileComp._outputTile == _outputTile && CurrentStackCount + floorTileComp.CurrentStackCount <= _maxStackCount)
                {
                    CurrentStackCount += floorTileComp.CurrentStackCount;
                    attacked.Delete();
                    return;
                }
                else
                {
                    return;
                }
            }

            //Assume user is trying to place a tile.
            var mapGrid = _mapManager.GetGrid(eventArgs.ClickLocation.GridID);
            var tile = mapGrid.GetTileRef(eventArgs.ClickLocation);

            var coordinates = mapGrid.GridTileToLocal(tile.GridIndices);
            float distance = coordinates.Distance(_mapManager, Owner.Transform.GridPosition);

            if (distance > InteractionSystem.InteractionRange)
            {
                return;
            }

            var tileDef = (ContentTileDefinition)_tileDefinitionManager[tile.Tile.TypeId];
            if (tileDef.IsSubFloor)
            {
                var desiredTile = _tileDefinitionManager[_outputTile];
                mapGrid.SetTile(eventArgs.ClickLocation, new Tile(desiredTile.TileId));
                _entitySystemManager.GetEntitySystem<AudioSystem>().Play("/Audio/items/genhit.ogg", Owner);

                CurrentStackCount--;
                if (CurrentStackCount <= 0)
                {
                    Owner.Delete();
                }
 
                
            }
        }

        public void Examine(FormattedMessage message)
        {
            var loc = IoCManager.Resolve<ILocalizationManager>();
            //TODO: Display stack size.
            message.AddMarkup(loc.GetString("A stack of [color=white]{0}[/color] floor tiles.", CurrentStackCount));
        }
    }
}
