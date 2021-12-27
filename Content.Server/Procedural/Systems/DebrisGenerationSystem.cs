using System;
using System.Collections.Generic;
using Content.Server.Procedural.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Random;

namespace Content.Server.Procedural.Systems;

public class DebrisGenerationSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ITileDefinitionManager _tileDefinition = default!;
    [Dependency] private IMapManager _mapManager = default!;

    public EntityUid GenerateDebris(DebrisPrototype proto, MapCoordinates location)
    {
        var (grid, gridEnt) = GenerateFloorplan(proto, location);
        proto.Populator.Populate(gridEnt, grid);
        return gridEnt;
    }

    /// <summary>
    /// Generates a new floorplan for the given debris.
    /// </summary>
    /// <returns></returns>
    public (IMapGrid, EntityUid) GenerateFloorplan(DebrisPrototype proto, MapCoordinates location)
    {
        var grid = _mapManager.CreateGrid(location.MapId);
        grid.WorldPosition = location.Position;

        switch (proto.FloorplanStyle)
        {
            case DebrisFloorplanStyle.Tiles:
                PlaceFloorplanTiles(proto, grid, false);
                break;
            case DebrisFloorplanStyle.Blobs:
                PlaceFloorplanTiles(proto, grid, true);
                break;
            default:
                throw new NotImplementedException();
        }

        return (grid, grid.GridEntityId);
    }

    private void PlaceFloorplanTiles(DebrisPrototype proto, IMapGrid grid, bool blobs)
    {
        // NO MORE THAN TWO ALLOCATIONS THANK YOU VERY MUCH.
        var spawnPoints = new HashSet<Vector2i>((int)proto.FloorPlacements * 4);
        var taken = new HashSet<Vector2i>((int)proto.FloorPlacements);

        void PlaceTile(Vector2i point)
        {
            // Assume we already know that the spawn point is safe.
            spawnPoints.Remove(point);
            taken.Add(point);
            var north = point.Offset(Direction.North);
            var south = point.Offset(Direction.South);
            var east = point.Offset(Direction.East);
            var west = point.Offset(Direction.West);
            var radsq = Math.Pow(proto.Radius, 2); // I'd put this outside but i'm not 100% certain caching it between calls is a gain.

            // The math done is essentially a fancy way of comparing the distance from 0,0 to the radius,
            // and skipping the sqrt normally needed for dist.
            if (!taken.Contains(north) && Math.Pow(north.X, 2) + Math.Pow(north.Y, 2)  <= radsq)
                spawnPoints.Add(north);
            if (!taken.Contains(south) && Math.Pow(south.X, 2) + Math.Pow(south.Y, 2)  <= radsq)
                spawnPoints.Add(south);
            if (!taken.Contains(east) && Math.Pow(east.X, 2) + Math.Pow(east.Y, 2)  <= radsq)
                spawnPoints.Add(east);
            if (!taken.Contains(west) && Math.Pow(west.X, 2) + Math.Pow(west.Y, 2)  <= radsq)
                spawnPoints.Add(west);

            grid.SetTile(point, new Tile(_tileDefinition[_random.Pick(proto.FloorTiles)].TileId));

        }

        PlaceTile(Vector2i.Zero);

        for (var i = 0; i < proto.FloorPlacements; i++)
        {
            var point = _random.Pick(spawnPoints);
            PlaceTile(point);

            if (blobs)
            {
                if (!taken.Contains(point.Offset(Direction.North)) && _random.Prob(0.5f))
                    PlaceTile(point.Offset(Direction.North));
                if (!taken.Contains(point.Offset(Direction.South)) && _random.Prob(0.5f))
                    PlaceTile(point.Offset(Direction.South));
                if (!taken.Contains(point.Offset(Direction.East)) && _random.Prob(0.5f))
                    PlaceTile(point.Offset(Direction.East));
                if (!taken.Contains(point.Offset(Direction.West)) && _random.Prob(0.5f))
                    PlaceTile(point.Offset(Direction.West));
            }
        }
    }


}
