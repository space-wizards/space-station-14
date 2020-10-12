using System;
using System.Threading;
using Content.Server.GameObjects.EntitySystems.DoAfter;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Maps;
using Content.Shared.Utility;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Items.RCD
{
    [RegisterComponent]
    public class RCDComponent : Component, IAfterInteract, IUse, IExamine
    {
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IServerEntityManager _serverEntityManager = default!;

        public override string Name => "RCD";
        private RcdMode _mode = 0; //What mode are we on? Can be floors, walls, deconstruct.
        private readonly RcdMode[] _modes = (RcdMode[])  Enum.GetValues(typeof(RcdMode));
        [ViewVariables(VVAccess.ReadWrite)] public int maxAmmo;
        public int _ammo; //How much "ammo" we have left. You can refill this with RCD ammo.
        [ViewVariables(VVAccess.ReadWrite)] private float _delay;
        private DoAfterSystem doAfterSystem;


        ///Enum to store the different mode states for clarity.
        private enum RcdMode
        {
            Floors,
            Walls,
            Airlock,
            Deconstruct
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref maxAmmo, "maxAmmo", 5);
            serializer.DataField(ref _delay, "delay", 2f);
        }

        public override void Initialize()
        {
            base.Initialize();
            _ammo = maxAmmo;
            doAfterSystem = EntitySystem.Get<DoAfterSystem>();
        }

        ///<summary>
        /// Method called when the RCD is clicked in-hand, this will cycle the RCD mode.
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
            _entitySystemManager.GetEntitySystem<AudioSystem>().PlayFromEntity("/Audio/Items/genhit.ogg", Owner);
            int mode = (int) _mode; //Firstly, cast our RCDmode mode to an int (enums are backed by ints anyway by default)
            mode = (++mode) % _modes.Length; //Then, do a rollover on the value so it doesnt hit an invalid state
            _mode = (RcdMode) mode; //Finally, cast the newly acquired int mode to an RCDmode so we can use it.
            Owner.PopupMessage(eventArgs.User, Loc.GetString("The RCD is now set to {0} mode.", _mode)); //Prints an overhead message above the RCD
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            message.AddMarkup(Loc.GetString("It's currently on {0} mode, and holds {1} charges.",_mode.ToString(), _ammo));
        }

        public async void AfterInteract(AfterInteractEventArgs   eventArgs)
        {
            //No changing mode mid-RCD
            var startingMode = _mode;

            var mapGrid = _mapManager.GetGrid(eventArgs.ClickLocation.GetGridId(Owner.EntityManager));
            var tile = mapGrid.GetTileRef(eventArgs.ClickLocation);
            var snapPos = mapGrid.SnapGridCellFor(eventArgs.ClickLocation, SnapGridOffset.Center);

            //Using an RCD isn't instantaneous
            var cancelToken = new CancellationTokenSource();
            var doAfterEventArgs = new DoAfterEventArgs(eventArgs.User, _delay, cancelToken.Token, eventArgs.Target)
            {
                BreakOnDamage = true,
                BreakOnStun = true,
                NeedHand = true,
                ExtraCheck = () => IsRCDStillValid(eventArgs, mapGrid, tile, snapPos, startingMode) //All of the sanity checks are here
            };

            var result = await doAfterSystem.DoAfter(doAfterEventArgs);
            if (result == DoAfterStatus.Cancelled)
            {
                return;
            }

            switch (_mode)
            {
                //Floor mode just needs the tile to be a space tile (subFloor)
                case RcdMode.Floors:
                    mapGrid.SetTile(eventArgs.ClickLocation, new Tile(_tileDefinitionManager["floor_steel"].TileId));
                    break;
                //We don't want to place a space tile on something that's already a space tile. Let's do the inverse of the last check.
                case RcdMode.Deconstruct:
                    if (!tile.IsBlockedTurf(true)) //Delete the turf
                    {
                        mapGrid.SetTile(snapPos, Tile.Empty);
                    }
                    else //Delete what the user targeted
                    {
                        eventArgs.Target.Delete();
                    }
                    break;
                //Walls are a special behaviour, and require us to build a new object with a transform rather than setting a grid tile, thus we early return to avoid the tile set code.
                case RcdMode.Walls:
                    var ent = _serverEntityManager.SpawnEntity("solid_wall", mapGrid.GridTileToLocal(snapPos));
                    ent.Transform.LocalRotation = Owner.Transform.LocalRotation; //Now apply icon smoothing.
                    break;
                case RcdMode.Airlock:
                    var airlock = _serverEntityManager.SpawnEntity("Airlock", mapGrid.GridTileToLocal(snapPos));
                    airlock.Transform.LocalRotation = Owner.Transform.LocalRotation; //Now apply icon smoothing.
                    break;
                default:
                    return; //I don't know why this would happen, but sure I guess. Get out of here invalid state!
            }

            _entitySystemManager.GetEntitySystem<AudioSystem>().PlayFromEntity("/Audio/Items/deconstruct.ogg", Owner);
            _ammo--;

        }

        private bool IsRCDStillValid(AfterInteractEventArgs eventArgs, IMapGrid mapGrid, TileRef tile, Vector2i snapPos, RcdMode startingMode)
        {
            //Less expensive checks first. Failing those ones, we need to check that the tile isn't obstructed.
            if (_ammo <= 0)
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("The RCD is out of ammo!"));
                return false;
            }

            if (_mode != startingMode)
            {
                return false;
            }

            var coordinates = mapGrid.ToCoordinates(tile.GridIndices);
            if (coordinates == EntityCoordinates.Invalid || !eventArgs.InRangeUnobstructed(ignoreInsideBlocker: true, popup: true))
            {
                return false;
            }

            switch (_mode)
            {
                //Floor mode just needs the tile to be a space tile (subFloor)
                case RcdMode.Floors:
                    if (!tile.Tile.IsEmpty)
                    {
                        Owner.PopupMessage(eventArgs.User, Loc.GetString("You can only build a floor on space!"));
                        return false;
                    }

                    return true;
                //We don't want to place a space tile on something that's already a space tile. Let's do the inverse of the last check.
                case RcdMode.Deconstruct:
                    if (tile.Tile.IsEmpty)
                    {
                        return false;
                    }

                    //They tried to decon a turf but the turf is blocked
                    if (eventArgs.Target == null && tile.IsBlockedTurf(true))
                    {
                        Owner.PopupMessage(eventArgs.User, Loc.GetString("That tile is obstructed!"));
                        return false;
                    }
                    //They tried to decon a non-turf but it's not in the whitelist
                    if (eventArgs.Target != null && !eventArgs.Target.TryGetComponent(out RCDDeconstructWhitelist rcd_decon))
                    {
                        Owner.PopupMessage(eventArgs.User, Loc.GetString("You can't deconstruct that!"));
                        return false;
                    }

                    return true;
                //Walls are a special behaviour, and require us to build a new object with a transform rather than setting a grid tile, thus we early return to avoid the tile set code.
                case RcdMode.Walls:
                    if (tile.Tile.IsEmpty)
                    {
                        Owner.PopupMessage(eventArgs.User, Loc.GetString("You cannot build a wall on space!"));
                        return false;
                    }

                    if (tile.IsBlockedTurf(true))
                    {
                        Owner.PopupMessage(eventArgs.User, Loc.GetString("That tile is obstructed!"));
                        return false;
                    }
                    return true;
                case RcdMode.Airlock:
                    if (tile.Tile.IsEmpty)
                    {
                        Owner.PopupMessage(eventArgs.User, Loc.GetString("Cannot build an airlock on space!"));
                        return false;
                    }
                    if (tile.IsBlockedTurf(true))
                    {
                        Owner.PopupMessage(eventArgs.User, Loc.GetString("That tile is obstructed!"));
                        return false;
                    }
                    return true;
                default:
                    return false; //I don't know why this would happen, but sure I guess. Get out of here invalid state!
            }
        }
    }
}
