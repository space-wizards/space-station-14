using System;
using Content.Server.GameObjects.EntitySystems.Click;
using Content.Server.Interfaces;
using Content.Server.Utility;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Maps;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
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
    public class RCDComponent : Component, IAfterInteract, IUse, IExamine
    {

#pragma warning disable 649
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
        [Dependency] private readonly IMapManager _mapManager;
        [Dependency] private readonly IServerEntityManager _serverEntityManager;
        [Dependency] private IServerNotifyManager _serverNotifyManager;
#pragma warning restore 649
        public override string Name => "RCD";
        private string _outputTile = "floor_steel";
        private RcdMode _mode = 0; //What mode are we on? Can be floors, walls, deconstruct.
        private readonly RcdMode[] _modes = (RcdMode[])  Enum.GetValues(typeof(RcdMode));
        private int _ammo = 5; //How much "ammo" we have left. You can refille this with RCD ammo.

        ///Enum to store the different mode states for clarity.
        private enum RcdMode
        {
            Floors,
            Walls,
            Deconstruct
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _outputTile, "output", "floor_steel");
        }


        ///<summary>
        /// Method called when the RCD is clicked in-hand, this will swap the RCD's mode from "floors" to "walls".
        ///</summary>

        public bool UseEntity(UseEntityEventArgs eventArgs)
        {
            SwapMode(eventArgs);
            return true;
        }

        ///<summary>
        ///Method to allow the user to swap the mode of the RCD by clicking it in hand, the actual in hand clicking bit is done over on UseEntity()
        ///@param UseEntityEventArgs = The entity which triggered this method call, used to know where to play the "click" sound.
        ///</summary>

        public void SwapMode(UseEntityEventArgs eventArgs)
        {
            _entitySystemManager.GetEntitySystem<AudioSystem>().PlayFromEntity("/Audio/items/genhit.ogg", Owner);
            int mode = (int) this._mode; //Firstly, cast our RCDmode mode to an int (enums are backed by ints anyway by default)
            mode = (++mode) % _modes.Length; //Then, do a rollover on the value so it doesnt hit an invalid state
            this._mode = (RcdMode) mode; //Finally, cast the newly acquired int mode to an RCDmode so we can use it.
            switch (this._mode)
            {
                case RcdMode.Floors:
                    _outputTile = "floor_steel";
                    break;
                case RcdMode.Walls:
                    _outputTile = "base_wall";
                    break;
                case RcdMode.Deconstruct:
                    _outputTile = "space";
                    break;
            }
            _serverNotifyManager.PopupMessage(Owner, eventArgs.User, $"The RCD is now set to {this._mode} mode."); //Prints an overhead message above the RCD
        }

        ///<summary>
        ///Method called when the user examines this object, it'll simply add the mode that it's in to the object's description
        ///@params message = The original message from examining, like ..() in BYOND's examine
        ///</summary>

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            message.AddMarkup(Loc.GetString("It's currently on {0} mode, and holds {1} charges.",_mode.ToString(), this._ammo));
        }

        ///<summary>
        /// Method to handle clicking on a tile to then appropriately RCD it. This can have several behaviours depending on mode.
        /// @param eventAargs = An action event telling us what tile was clicked on. We use this to exrapolate where to place the new tile / remove the old one etc.
        ///</summary>

        public void AfterInteract(AfterInteractEventArgs   eventArgs)
        {
            var mapGrid = _mapManager.GetGrid(eventArgs.ClickLocation.GridID);
            var tile = mapGrid.GetTileRef(eventArgs.ClickLocation);
            var coordinates = mapGrid.GridTileToLocal(tile.GridIndices);
            //Less expensive checks first. Failing those ones, we need to check that the tile isn't obstructed.
            if (_ammo <= 0 || coordinates == GridCoordinates.InvalidGrid || !InteractionChecks.InRangeUnobstructed(eventArgs))
            {
                return;
            }

            var targetTile = (ContentTileDefinition) _tileDefinitionManager[tile.Tile.TypeId];

            var canPlaceTile = targetTile.IsSubFloor; //Boolean to check if we're able to build the desired tile. This defaults to checking for subfloors, but is overridden by "deconstruct" which sets it to the inverse.

            switch (this._mode)
            {
                //Floor mode just needs the tile to be a space tile (subFloor)
                case RcdMode.Floors:
                    break;
                //We don't want to place a space tile on something that's already a space tile. Let's do the inverse of the last check.
                case RcdMode.Deconstruct:
                    canPlaceTile = !targetTile.IsSubFloor;
                    break;
                //Walls are a special behaviour, and require us to build a new object with a transform rather than setting a grid tile, thus we early return to avoid the tile set code.
                case RcdMode.Walls:
                    var snapPos = mapGrid.SnapGridCellFor(eventArgs.ClickLocation, SnapGridOffset.Center);
                    var ent = _serverEntityManager.SpawnEntity("solid_wall", mapGrid.GridTileToLocal(snapPos));
                    ent.Transform.LocalRotation = Owner.Transform.LocalRotation; //Now apply icon smoothing.
                    _entitySystemManager.GetEntitySystem<AudioSystem>().PlayFromEntity("/Audio/items/deconstruct.ogg", Owner);
                    _ammo--;
                    return; //Alright we're done here
                default:
                    return; //I don't know why this would happen, but sure I guess. Get out of here invalid state!
            }

            ITileDefinition desiredTile = null;
            desiredTile = _tileDefinitionManager[_outputTile];
            if (canPlaceTile) //If desiredTile is null by this point, something has gone horribly wrong and you need to fix it.
            {
                mapGrid.SetTile(eventArgs.ClickLocation, new Tile(desiredTile.TileId));
                _entitySystemManager.GetEntitySystem<AudioSystem>().PlayFromEntity("/Audio/items/deconstruct.ogg", Owner);
                _ammo--;
            }
        }
    }
}
