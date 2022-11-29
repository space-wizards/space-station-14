using Content.Server.Salvage.Expeditions;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Salvage;

public sealed partial class SalvageSystem
{
    private SalvageJob GetCaveJob(EntityUid uid, MapGridComponent component, SalvageExpeditionPrototype expedition, SalvageCaveGen cave, int seed)
    {
        // We'll do the CA generation up front as we can do that pretty quickly
        // All of the spawning and other setup we'll run over multiple ticks as it will likely go over.

        // Salvage stuff
        var width = cave.Width;
        var height = cave.Height;
        var length = width * height;

        // CA stuff
        var solidChance = 0.4;

        // Need to double buffer
        Span<bool> cells1 = stackalloc bool[width * height];
        Span<bool> cells2 = stackalloc bool[width * height];
        var random = new Random(seed);

        for (var i = 0; i < length; i++)
        {
            if (random.NextDouble() <= solidChance)
            {
                cells1[i] = true;
            }
            else
            {
                cells1[i] = false;
            }
        }

        var simSteps = 2;
        var useCells = cells1;

        for (var i = 0; i < simSteps; i++)
        {
            // Swap the double buffer
            if (i % 2 != 0)
            {
                var tempCells = cells1;
                cells1 = cells2;
                cells2 = tempCells;
                useCells = cells2;
            }

            Step(cells1, cells2, width, height);
        }

        var tiles = new List<(Vector2i Indices, Tile Tile)>(cells1.Length);
        var tile = new Tile(_tileDefManager["FloorAsteroidIronsand1"].TileId);

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                var cell = useCells[y * height + x];

                if (!cell)
                    continue;

                tiles.Add(new ValueTuple<Vector2i, Tile>(new Vector2i(x, y), tile));
            }
        }

        component.SetTiles(tiles);
        // TODO: Set asteroids... inside the job.

        return new SalvageJob(EntityManager, uid, tiles, seed, SalvageGenTime);
    }

    private void Step(Span<bool> cells1, Span<bool> cells2, int width, int height)
    {
        var aliveLimit = 4;
        var deadLimit = 3;

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                // Count the cell neighbours
                var count = 0;

                for (var i = -1; i < 2; i++)
                {
                    for (var j = -1; j < 2; j++)
                    {
                        if (i == 0 && j == 0)
                            continue;

                        var neighborX = x + i;
                        var neighborY = y + j;

                        // Out of bounds
                        if (neighborX < 0 || neighborX >= width ||
                            neighborY < 0 || neighborY >= height)
                        {
                            continue;
                        }

                        if (cells1[neighborY * height + neighborX])
                        {
                            count++;
                        }
                    }
                }

                // Alright now check the thresholds to see what to do with the cell
                var index = y * height + x;
                var alive = cells1[index];

                if (alive)
                {
                    if (count < deadLimit)
                    {
                        cells2[index] = false;
                    }
                    else
                    {
                        cells2[index] = true;
                    }
                }
                else
                {
                    if (count > aliveLimit)
                    {
                        cells2[index] = true;
                    }
                    else
                    {
                        cells2[index] = false;
                    }
                }
            }
        }
    }
}
