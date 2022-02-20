using System;
using System.Collections.Generic;
using Content.Shared.SubFloor;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using static Robust.Client.Graphics.RSI.State;
using static Robust.UnitTesting.RobustIntegrationTest;

namespace Content.MapRenderer.Painters;

public sealed class EntityPainter
{
    private readonly IResourceCache _cResourceCache;

    private readonly Dictionary<(string path, string state), Image> _images;
    private readonly Image _errorImage;

    private readonly IEntityManager _sEntityManager;

    public EntityPainter(ClientIntegrationInstance client, ServerIntegrationInstance server)
    {
        _cResourceCache = client.ResolveDependency<IResourceCache>();

        _sEntityManager = server.ResolveDependency<IEntityManager>();

        _images = new Dictionary<(string path, string state), Image>();
        _errorImage = Image.Load<Rgba32>(_cResourceCache.ContentFileRead("/Textures/error.rsi/error.png"));
    }

    public void Run(Image canvas, List<EntityData> entities)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        // TODO cache this shit what are we insane
        entities.Sort(Comparer<EntityData>.Create((x, y) => x.Sprite.DrawDepth.CompareTo(y.Sprite.DrawDepth)));

        foreach (var entity in entities)
        {
            Run(canvas, entity);
        }

        Console.WriteLine($"{nameof(GridPainter)} painted {entities.Count} entities in {(int) stopwatch.Elapsed.TotalMilliseconds} ms");
    }

    public void Run(Image canvas, EntityData entity)
    {
        if (_sEntityManager.HasComponent<SubFloorHideComponent>(entity.Sprite.Owner))
        {
            return;
        }

        if (!entity.Sprite.Visible || entity.Sprite.ContainerOccluded)
        {
            return;
        }

        var worldRotation = _sEntityManager.GetComponent<TransformComponent>(entity.Sprite.Owner).WorldRotation;
        foreach (var layer in entity.Sprite.AllLayers)
        {
            if (!layer.Visible)
            {
                continue;
            }

            if (!layer.RsiState.IsValid)
            {
                continue;
            }

            var rsi = layer.ActualRsi;
            Image image;

            if (rsi == null || rsi.Path == null || !rsi.TryGetState(layer.RsiState, out var state))
            {
                image = _errorImage;
            }
            else
            {
                var key = (rsi.Path!.ToString(), state.StateId.Name!);

                if (!_images.TryGetValue(key, out image!))
                {
                    var stream = _cResourceCache.ContentFileRead($"{rsi.Path}/{state.StateId}.png");
                    image = Image.Load<Rgba32>(stream);

                    _images[key] = image;
                }
            }

            image = image.CloneAs<Rgba32>();

            var directions = entity.Sprite.GetLayerDirectionCount(layer);

            // TODO add support for 8 directions and animations (delays)
            if (directions != 1 && directions != 8)
            {
                double xStart, xEnd, yStart, yEnd;

                switch (directions)
                {
                    case 4:
                    {
                        var dir = layer.EffectiveDirection(worldRotation);

                        (xStart, xEnd, yStart, yEnd) = dir switch
                        {
                            // Only need the first tuple as doubles for the compiler to recognize it
                            Direction.South => (0d, 0.5d, 0d, 0.5d),
                            Direction.East => (0, 0.5, 0.5, 1),
                            Direction.North => (0.5, 1, 0, 0.5),
                            Direction.West => (0.5, 1, 0.5, 1),
                            _ => throw new ArgumentOutOfRangeException(nameof(dir))
                        };
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                var x = (int) (image.Width * xStart);
                var width = (int) (image.Width * xEnd) - x;

                var y = (int) (image.Height * yStart);
                var height = (int) (image.Height * yEnd) - y;

                image.Mutate(o => o.Crop(new Rectangle(x, y, width, height)));
            }

            var colorMix = entity.Sprite.Color * layer.Color;
            var imageColor = Color.FromRgba(colorMix.RByte, colorMix.GByte, colorMix.BByte, colorMix.AByte);
            var coloredImage = new Image<Rgba32>(image.Width, image.Height);
            coloredImage.Mutate(o => o.BackgroundColor(imageColor));

            image.Mutate(o => o
                .DrawImage(coloredImage, PixelColorBlendingMode.Multiply, PixelAlphaCompositionMode.SrcAtop, 1)
                .Resize(32, 32)
                .Flip(FlipMode.Vertical));

            var pointX = (int) entity.X;
            var pointY = (int) entity.Y;
            canvas.Mutate(o => o.DrawImage(image, new Point(pointX, pointY), 1));
        }
    }
}
