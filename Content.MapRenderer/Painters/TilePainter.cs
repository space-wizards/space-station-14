using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using static Robust.UnitTesting.RobustIntegrationTest;

namespace Content.MapRenderer.Painters
{
    public sealed class TilePainter
    {
        public const int TileImageSize = EyeManager.PixelsPerMeter;

        private readonly ITileDefinitionManager _sTileDefinitionManager;
        private readonly SharedMapSystem _sMapSystem;
        private readonly IResourceManager _resManager;

        public TilePainter(ClientIntegrationInstance client, ServerIntegrationInstance server)
        {
            _sTileDefinitionManager = server.ResolveDependency<ITileDefinitionManager>();
            _resManager = client.ResolveDependency<IResourceManager>();
            var esm = server.ResolveDependency<IEntitySystemManager>();
            _sMapSystem = esm.GetEntitySystem<SharedMapSystem>();
        }

        public void Run(Image gridCanvas, EntityUid gridUid, MapGridComponent grid, Vector2 customOffset = default)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var bounds = grid.LocalAABB;
            var xOffset = -bounds.Left;
            var yOffset = -bounds.Bottom;
            var tileSize = grid.TileSize * TileImageSize;

            var images = GetTileImages(_sTileDefinitionManager, _resManager, tileSize);
            var i = 0;

            _sMapSystem.GetAllTiles(gridUid, grid).AsParallel().ForAll(tile =>
            {
                var path = _sTileDefinitionManager[tile.Tile.TypeId].Sprite.ToString();

                if (string.IsNullOrWhiteSpace(path))
                    return;

                var x = (int) (tile.X + xOffset + customOffset.X);
                var y = (int) (tile.Y + yOffset + customOffset.Y);
                var image = images[path][tile.Tile.Variant].CloneAs<Rgba32>();

                switch (tile.Tile.RotationMirroring % 4)
                {
                    case 0:
                        break;
                    case 1:
                        image.Mutate(o => o.Rotate(90f));
                        break;
                    case 2:
                        image.Mutate(o => o.Rotate(180f));
                        break;
                    case 3:
                        image.Mutate(o => o.Rotate(270f));
                        break;
                }

                if (tile.Tile.RotationMirroring > 3)
                {
                    image.Mutate(o => o.Flip(FlipMode.Horizontal));
                }

                gridCanvas.Mutate(o => o.DrawImage(image, new Point(x * tileSize, y * tileSize), 1));

                i++;
            });

            Console.WriteLine($"{nameof(TilePainter)} painted {i} tiles on grid {gridUid} in {(int) stopwatch.Elapsed.TotalMilliseconds} ms");
        }

        private Dictionary<string, List<Image>> GetTileImages(
            ITileDefinitionManager tileDefinitionManager,
            IResourceManager resManager,
            int tileSize)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var images = new Dictionary<string, List<Image>>();

            foreach (var definition in tileDefinitionManager)
            {
                var path = definition.Sprite.ToString();

                if (string.IsNullOrWhiteSpace(path))
                    continue;

                images[path] = new List<Image>(definition.Variants);

                using var stream = resManager.ContentFileRead(path);
                Image tileSheet = Image.Load<Rgba32>(stream);

                if (tileSheet.Width != tileSize * definition.Variants || tileSheet.Height != tileSize)
                {
                    throw new NotSupportedException($"Unable to use tiles with a dimension other than {tileSize}x{tileSize}.");
                }

                for (var i = 0; i < definition.Variants; i++)
                {
                    var index = i;
                    var tileImage = tileSheet.Clone(o => o.Crop(new Rectangle(tileSize * index, 0, 32, 32)).Flip(FlipMode.Vertical));
                    images[path].Add(tileImage);
                }
            }

            Console.WriteLine($"Indexed all tile images in {(int) stopwatch.Elapsed.TotalMilliseconds} ms");

            return images;
        }
    }
}
