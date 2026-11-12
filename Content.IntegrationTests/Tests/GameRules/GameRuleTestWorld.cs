using System.Collections.Generic;
using Content.IntegrationTests.Pair;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests.GameRules;

/// <summary>
/// Absolute bare minimum station for test purposes.
/// </summary>
public static class GameRuleTestWorld
{
    public static async Task Prepare(TestPair pair)
    {
        await pair.Server.WaitPost(() =>
        {
            var server = pair.Server;
            var entities = server.EntMan;
            var player = pair.Player!.AttachedEntity!.Value;
            var transform = entities.GetComponent<TransformComponent>(player);
            var grid = transform.GridUid!.Value;
            var maps = server.System<SharedMapSystem>();
            var floor = server.Resolve<ITileDefinitionManager>()["FloorSteel"].TileId;
            var tiles = new List<(Vector2i, Tile)>();
            for (var x = -10; x <= 10; x++)
            {
                for (var y = -10; y <= 10; y++)
                {
                    tiles.Add((new Vector2i(x, y), new Tile(floor)));
                }
            }

            maps.SetTiles(grid, entities.GetComponent<MapGridComponent>(grid), tiles);

            var moles = new float[Atmospherics.AdjustedNumberOfGases];
            moles[(int) Gas.Oxygen] = 21.824779f;
            moles[(int) Gas.Nitrogen] = 82.10312f;
            var air = new GasMixture(moles, Atmospherics.T20C);
            server.System<AtmosphereSystem>().SetMapAtmosphere(transform.MapUid!.Value, false, air);

            // Needed for events funnily enough
            for (var x = 0; x < 8; x++)
            {
                entities.SpawnAtPosition("GasVentPump", new EntityCoordinates(grid, x + 0.5f, 2.5f));
            }

            entities.SpawnAtPosition("GasVentScrubber", new EntityCoordinates(grid, 3.5f, 3.5f));
        });
        
        await pair.RunUntilSynced();
    }
}
