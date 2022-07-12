using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using static Robust.UnitTesting.RobustIntegrationTest;

namespace Content.MapRenderer.Painters
{
    public sealed class TilePainter
    {
        private const string TilesPath = "/Textures/Tiles/";
        public const int TileImageSize = EyeManager.PixelsPerMeter;

        private readonly ITileDefinitionManager _sTileDefinitionManager;
        private readonly IResourceCache _cResourceCache;

        public TilePainter(ClientIntegrationInstance client, ServerIntegrationInstance server)
        {
            _sTileDefinitionManager = server.ResolveDependency<ITileDefinitionManager>();
            _cResourceCache = client.ResolveDependency<IResourceCache>();
        }

        public void Run(Image gridCanvas, IMapGrid grid)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var bounds = grid.LocalAABB;
            var xOffset = -bounds.Left;
            var yOffset = -bounds.Bottom;
            var tileSize = grid.TileSize * TileImageSize;

            var images = GetTileImages(_sTileDefinitionManager, _cResourceCache, tileSize);
            var i = 0;

            grid.GetAllTiles().AsParallel().ForAll(tile =>
            {
                var x = (int) (tile.X + xOffset);
                var y = (int) (tile.Y + yOffset);
                var sprite = _sTileDefinitionManager[tile.Tile.TypeId].SpriteName;
                var image = images[sprite][tile.Tile.Variant];

                gridCanvas.Mutate(o => o.DrawImage(image, new Point(x * tileSize, y * tileSize), 1));

                i++;
            });

            Console.WriteLine($"{nameof(TilePainter)} painted {i} tiles on grid {grid.GridEntityId} in {(int) stopwatch.Elapsed.TotalMilliseconds} ms");
        }

        private Dictionary<string, List<Image>> GetTileImages(
            ITileDefinitionManager tileDefinitionManager,
            IResourceCache resourceCache,
            int tileSize)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var images = new Dictionary<string, List<Image>>();

            foreach (var definition in tileDefinitionManager)
            {
                var sprite = definition.SpriteName;
                images[sprite] = new List<Image>(definition.Variants);

                if (string.IsNullOrEmpty(sprite))
                {
                    continue;
                }

                using var stream = resourceCache.ContentFileRead($"{TilesPath}{sprite}.png");
                Image tileSheet = Image.Load<Rgba32>(stream);

                if (tileSheet.Width != tileSize * definition.Variants || tileSheet.Height != tileSize)
                {
                    throw new NotSupportedException($"Unable to use tiles with a dimension other than {tileSize}x{tileSize}.");
                }

                for (var i = 0; i < definition.Variants; i++)
                {
                    var tileImage = tileSheet.Clone(o => o.Crop(new Rectangle(tileSize * i, 0, 32, 32)));
                    images[sprite].Add(tileImage);
                }
            }

            Console.WriteLine($"Indexed all tile images in {(int) stopwatch.Elapsed.TotalMilliseconds} ms");

            return images;
        }
    }
}
