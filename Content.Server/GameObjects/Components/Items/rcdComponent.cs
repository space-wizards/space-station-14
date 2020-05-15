using System;
using Content.Server.GameObjects.Components.Stack;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Shared.Maps;
using Microsoft.EntityFrameworkCore.Internal;
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
#pragma warning restore 649
        public override string Name => "RCD";
        public string _outputTile = "floor_steel";
        String[] modes = Enum.GetNames(typeof(RCDmodes)); //Displayed modes for saying stuff like "you switch to floors mode"
        private int mode = 0; //What mode are we on? Can be floors, walls, deconstruct.
        private int ammo = 5; //How much "ammo" we have left. You can refille this with RCD ammo.

        private enum RCDmodes //Enum to store the different mode states for clarity.
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
            this.mode = (this.mode < modes.Length-1) ? this.mode + 1 : 0; //Basic value rollover so that you can't get to an invalid mode
            string mode = modes[this.mode];
            switch (mode)
            {
                case("Floors"):
                    _outputTile = "floor_steel";
                    break;
                case("Walls"):
                    _outputTile = "base_wall";
                    break;
                case("Deconstruct"):
                    _outputTile = "space";
                    break;
            }
            var notify = IoCManager.Resolve<IServerNotifyManager>();
            notify.PopupMessage(Owner, eventArgs.User, "The RCD will now place "+mode); //Prints an overhead message above the RCD
        }

        /**
         *
         * Method called when the user examines this object, it'll simply add the mode that it's in to the object's description
         * @params message = The original message from examining, like ..() in BYOND's examine
         *
         */

        void IExamine.Examine(FormattedMessage message)
        {
            string mode = Enum.GetNames(typeof(RCDmodes))[this.mode]; //Access the string name of the mode based off of its numerical index in the enum.
            message.AddMarkup(Loc.GetString("It's currently placing "+mode+", and holds "+this.ammo+" charges."));
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

                string mode = modes[this.mode];
                if (mode == "Floors" || mode == "Deconstruct")
                {
                    if (!tileDef.IsSubFloor || attacked != null)
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
