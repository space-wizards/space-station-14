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
    public class TilePainter
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

            var bounds = grid.LocalBounds;
            var xOffset = Math.Abs(bounds.Left);
            var yOffset = Math.Abs(bounds.Bottom);
            var tileSize = grid.TileSize * TileImageSize;

            var images = GetTileImages(_sTileDefinitionManager, _cResourceCache, tileSize);
            var i = 0;

            grid.GetAllTiles().AsParallel().ForAll(tile =>
            {
                var x = (int) (tile.X + xOffset);
                var y = (int) (tile.Y + yOffset);
                var sprite = _sTileDefinitionManager[tile.Tile.TypeId].SpriteName;
                var image = images[sprite];

                gridCanvas.Mutate(o => o.DrawImage(image, new Point(x * tileSize, y * tileSize), 1));

                i++;
            });

            Console.WriteLine($"{nameof(TilePainter)} painted {i} tiles on grid {grid.Index} in {(int) stopwatch.Elapsed.TotalMilliseconds} ms");
        }

        private Dictionary<string, Image> GetTileImages(
            ITileDefinitionManager tileDefinitionManager,
            IResourceCache resourceCache,
            int tileSize)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var images = new Dictionary<string, Image>();

            foreach (var definition in tileDefinitionManager)
            {
                var sprite = definition.SpriteName;

                if (string.IsNullOrEmpty(sprite))
                {
                    continue;
                }

                using var stream = resourceCache.ContentFileRead($"{TilesPath}{sprite}.png");
                Image tileImage = Image.Load<Rgba32>(stream);

                if (tileImage.Width != tileSize || tileImage.Height != tileSize)
                {
                    throw new NotSupportedException($"Unable to use tiles with a dimension other than {tileSize}x{tileSize}.");
                }

                images[sprite] = tileImage;
            }

            Console.WriteLine($"Indexed all tile images in {(int) stopwatch.Elapsed.TotalMilliseconds} ms");

            return images;
        }
    }
}
