using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.Interfaces;
using Content.Shared.Chemistry;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Fluids
{
    [RegisterComponent]
    class SprayComponent : Component, IAfterInteract
    {
#pragma warning disable 649
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IServerNotifyManager _notifyManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
#pragma warning restore 649
        public override string Name => "Spray";

        private ReagentUnit _transferAmount;
        private int _sprayRange;
        private string _spraySound;

        /// <summary>
        ///     The amount of solution to be sprayer from this solution when using it
        /// </summary>
        [ViewVariables]
        public ReagentUnit TransferAmount
        {
            get => _transferAmount;
            set => _transferAmount = value;
        }

        private SolutionComponent _contents;
        public ReagentUnit CurrentVolume => _contents.CurrentVolume;

        public override void Initialize()
        {
            base.Initialize();
            _contents = Owner.GetComponent<SolutionComponent>();
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _transferAmount, "transferAmount", ReagentUnit.New(5.0));
            serializer.DataField(ref _sprayRange, "sprayRange", 3);
            serializer.DataField(ref _spraySound, "spraySound", string.Empty);
        }

        // Source: https://rosettacode.org/wiki/Bitmap/Bresenham%27s_line_algorithm#C.23
        // Gets all tiles in between origin and end, plus the end tile itself
        List<GridCoordinates> GetTilesBetween(GridCoordinates origin, GridCoordinates end, int range = 100)
        {
            var tiles = new List<GridCoordinates>();
            int dx = Math.Abs((int)(end.X - origin.X)), sx = origin.X < end.X ? 1 : -1;
            int dy = Math.Abs((int)(end.Y - origin.Y)), sy = origin.Y < end.Y ? 1 : -1;
            int err = (dx > dy ? dx : -dy) / 2, e2;
            for (int i = 0; i < range; i++)
            {
                e2 = err;
                if (e2 > -dx) { err -= dy; origin = origin.Offset(new Vector2(sx, 0)); }
                if (e2 < dy) { err += dx; origin = origin.Offset(new Vector2(0, sy)); }
                // Add tile to list and check if we're (prematurely) at the end
                tiles.Add(origin);
                if (origin.X == end.X && origin.Y == end.Y) break;
            }
            return tiles;
        }

        void IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (CurrentVolume <= 0)
            {
                _notifyManager.PopupMessage(Owner, eventArgs.User, Loc.GetString("It's empty!"));
                return;
            }

            var playerPos = eventArgs.User.Transform.GridPosition;
            if (eventArgs.ClickLocation.GridID != playerPos.GridID)
                return;

            var mapGrid = _mapManager.GetGrid(eventArgs.ClickLocation.GridID);

            var clickTile = mapGrid.GetTileRef(eventArgs.ClickLocation);
            var clickCoordinates = mapGrid.GridTileToLocal(clickTile.GridIndices);

            var playerTile = mapGrid.GetTileRef(playerPos);
            var playerCoord = mapGrid.GridTileToLocal(playerTile.GridIndices);

            // Get the tiles we want to spray things at
            //TODO: if you spray directly against a wall, you should spray the ground instead?
            List<GridCoordinates> tiles;
            if (clickCoordinates == playerCoord) // clicked on the tile standing on
            {
                tiles = new List<GridCoordinates>() { playerCoord };
            }
            else
            {
                tiles = GetTilesBetween(playerCoord, clickCoordinates, _sprayRange);
            }

            //Play sound
            EntitySystem.Get<AudioSystem>().PlayFromEntity(_spraySound, Owner);

            //TODO: add spray visual effect?
            // Spray the tiles, half the amount each step
            ReagentUnit amount = TransferAmount;
            foreach (var tile in tiles)
            {
                if (CurrentVolume == 0 || Math.Round(amount.Float()) == 0) // no need to create empty puddles
                    break;

                var mapCoords = tile.ToMap(_mapManager);

                // Check if any impassable & hard entities on the tile (e.g. walls, vending machines)
                var ents = _entityManager.GetEntitiesAt(mapCoords.MapId, mapCoords.Position, true);
                var unobstructed = true;
                foreach (var ent in ents)
                {
                    if (ent.TryGetComponent(out ICollidableComponent coll))
                    {
                        if ((coll.CollisionLayer & (int)Content.Shared.Physics.CollisionGroup.Impassable) != 0 && coll.Hard)
                        {
                            unobstructed = false;
                            break;
                        }
                    }
                }
                if (unobstructed)
                {
                    VaporHelper.SpillAt(tile, _contents.SplitSolution(amount));
                    amount = amount * 0.5;
                }
                else // found wall and that stops the spray
                {
                    break;
                }
            }
        }
    }
}
