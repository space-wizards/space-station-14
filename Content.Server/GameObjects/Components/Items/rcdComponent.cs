using System;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Shared.Maps;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.Components.Items
{
    [RegisterComponent]
    public class RcdComponent : Component, IAfterAttack, IUse, IExamine
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
        private RCDmode mode = 0; //What mode are we on? Can be floors, walls, deconstruct.
        private Array modes = Enum.GetValues(typeof(RCDmode));
        private int ammo = 5; //How much "ammo" we have left. You can refille this with RCD ammo.

        private enum RCDmode //Enum to store the different mode states for clarity.
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

        /**
         *
         *    Method called when the RCD is clicked in-hand, this will swap the RCD's mode from "floors" to "walls".
         *
         */

        public bool UseEntity(UseEntityEventArgs eventArgs)
        {
            SwapMode(eventArgs);
            return true;
        }


        /**
         *
         * Un-used method which is inherited from super
         *
         */
        public void Activate(ActivateEventArgs eventArgs)
        {
            return;
        }

        /**
         *
         * Method to allow the user to swap the mode of the RCD by clicking it in hand, the actual in hand clicking bit is done over on UseEntity()
         *
         * @param UseEntityEventArgs = The entity which triggered this method call, used to know where to play the "click" sound.
         *
         */

        public void SwapMode(UseEntityEventArgs eventArgs)
        {
            _entitySystemManager.GetEntitySystem<AudioSystem>().Play("/Audio/items/genhit.ogg", Owner);
            int mode = (int) this.mode; //Firstly, cast our RCDmode mode to an int (enums are backed by ints anyway by default)
            mode = (++mode) % modes.Length; //Then, do a rollover on the value so it doesnt hit an invalid state
            this.mode = (RCDmode) mode; //Finally, cast the newly acquired int mode to an RCDmode so we can use it.
            switch (this.mode)
            {
                case RCDmode.Floors:
                    _outputTile = "floor_steel";
                    break;
                case RCDmode.Walls:
                    _outputTile = "base_wall";
                    break;
                case RCDmode.Deconstruct:
                    _outputTile = "space";
                    break;
            }
            _serverNotifyManager.PopupMessage(Owner, eventArgs.User, "The RCD is now set to "+this.mode+" mode."); //Prints an overhead message above the RCD
        }

        /**
         *
         * Method called when the user examines this object, it'll simply add the mode that it's in to the object's description
         * @params message = The original message from examining, like ..() in BYOND's examine
         *
         */

        void IExamine.Examine(FormattedMessage message)
        {
            message.AddMarkup(Loc.GetString("It's currently in "+mode+" mode, and holds "+this.ammo+" charges."));
        }

        /**
         *
         * Method to handle clicking on a tile to then appropriately RCD it. This can have several behaviours depending on mode.
         * @param eventAargs = An action event telling us what tile was clicked on. We use this to exrapolate where to place the new tile / remove the old one etc.
         *
         */

        public void AfterAttack(AfterAttackEventArgs eventArgs)
        {
            var attacked = eventArgs.Attacked;
            var mapGrid = _mapManager.GetGrid(eventArgs.ClickLocation.GridID);
            var tile = mapGrid.GetTileRef(eventArgs.ClickLocation);

            var coordinates = mapGrid.GridTileToLocal(tile.GridIndices);
            float distance = coordinates.Distance(_mapManager, Owner.Transform.GridPosition);

            if (distance > InteractionSystem.InteractionRange || ammo <= 0)
            {
                return;
            }

            var tileDef = (ContentTileDefinition)_tileDefinitionManager[tile.Tile.TypeId];

                if (mode == RCDmode.Floors || mode == RCDmode.Deconstruct)
                {
                    if ((mode == RCDmode.Floors && !tileDef.IsSubFloor) || attacked != null) //Deconstruct mode can rip up any tile, but we don't want to place floor tiles on pre-existing floor tiles.
                    {
                        return;
                    }
                    var desiredTile = _tileDefinitionManager[_outputTile];
                    mapGrid.SetTile(eventArgs.ClickLocation, new Tile(desiredTile.TileId));
                }
                else
                {
                    if (!_mapManager.TryGetGrid(eventArgs.ClickLocation.GridID, out var grid))
                    {
                        return;
                    }
                    var snapPos = grid.SnapGridCellFor(eventArgs.ClickLocation, SnapGridOffset.Center);
                    GridCoordinates snapCoords = grid.GridTileToLocal(snapPos);
                    var ent = _serverEntityManager.SpawnEntity("solid_wall", snapCoords);
                    ent.GetComponent<ITransformComponent>().LocalRotation = Owner.GetComponent<ITransformComponent>().LocalRotation; //Now apply icon smoothing.

                }
                _entitySystemManager.GetEntitySystem<AudioSystem>().Play("/Audio/items/deconstruct.ogg", Owner);
                ammo--;
        }
    }
}
