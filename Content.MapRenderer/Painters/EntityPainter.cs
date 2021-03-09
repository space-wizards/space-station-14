using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Content.Client.GameObjects.Components;
using Robust.Client.ResourceManagement;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using static Robust.UnitTesting.RobustIntegrationTest;
using SpriteComponent = Robust.Client.GameObjects.SpriteComponent;

namespace Content.MapRenderer.Painters
{
    public class EntityPainter
    {
        private readonly ClientIntegrationInstance _client;

        private readonly IPrototypeManager _cPrototypeManager;
        private readonly IResourceCache _cResourceCache;
        private readonly IEntityManager _cEntityManager;
        private readonly IComponentManager _sComponentManager;
        private readonly IEntityManager _sEntityManager;

        private readonly ConcurrentDictionary<GridId, List<EntityData>> _entities;

        public EntityPainter(ClientIntegrationInstance client, ServerIntegrationInstance server)
        {
            _client = client;
            _cPrototypeManager = client.ResolveDependency<IPrototypeManager>();
            _cResourceCache = client.ResolveDependency<IResourceCache>();
            _cEntityManager = client.ResolveDependency<IEntityManager>();
            _sComponentManager = server.ResolveDependency<IComponentManager>();
            _sEntityManager = server.ResolveDependency<IEntityManager>();
            _entities = GetEntities();
        }

        public void Run(Image gridCanvas, IMapGrid grid)
        {
            if (!_entities.TryGetValue(grid.Index, out var walls))
            {
                Console.WriteLine($"No walls found on grid {grid.Index}");
                return;
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var bounds = grid.WorldBounds;
            var xOffset = (int) Math.Abs(bounds.Left);
            var yOffset = (int) Math.Abs(bounds.Bottom);


            // TODO cache this shit what are we insane
            walls.Sort(Comparer<EntityData>.Create((x, y) => x.Sprite.DrawDepth.CompareTo(y.Sprite.DrawDepth)));

            foreach (var entity in walls.AsParallel())
            {
                if (entity.Sprite.Owner.HasComponent<SubFloorHideComponent>())
                {
                    continue;
                }

                if (!entity.Sprite.Visible || entity.Sprite.ContainerOccluded)
                {
                    continue;
                }

                var rotation = entity.Sprite.Owner.Transform.WorldRotation;
                var position = entity.Sprite.Owner.Transform.WorldPosition;

                foreach (var layer in entity.Sprite.AllLayers)
                {
                    if (!layer.Visible)
                    {
                        continue;
                    }

                    if (layer.RsiState.IsValid)
                    {
                        // Pull texture from RSI state instead.
                        var rsi = layer.ActualRsi;
                        if (rsi == null || !rsi.TryGetState(layer.RsiState, out var state))
                        {
                            state = _cResourceCache.GetResource<RSIResource>("/Textures/error.rsi").RSI["error"];
                        }

                        var stateId = state.StateId;
                        Stream stream;

                        if (!_cResourceCache.TryContentFileRead($"{rsi.Path}/full.png", out stream))
                        {
                            if (rsi.Path.ToString().EndsWith("low_wall.rsi"))
                            {
                                stream = _cResourceCache.ContentFileRead($"{rsi.Path}/metal.png");
                            }
                            else if (rsi.Path.ToString().EndsWith("catwalk.rsi"))
                            {
                                stream = _cResourceCache.ContentFileRead($"{rsi.Path}/catwalk_preview.png");
                            }
                            else
                            {
                                stream = _cResourceCache.ContentFileRead($"{rsi.Path}/{stateId}.png");
                            }
                        }


                        var image = Image.Load<Rgba32>(stream);

                        image.Mutate(o => o.Resize(32, 32).Flip(FlipMode.Vertical));

                        gridCanvas.Mutate(o => o.DrawImage(image, new Point((entity.X + xOffset) * 32, (entity.Y + yOffset) * 32), 1));
                    }
                }

                // if (wall.Specifiers.Count == 0)
                // {
                //     continue;
                // }
                //
                // if (wall.Specifiers[0] is not Rsi rsi)
                // {
                //     continue;
                // }
                //
                // Image image;
                //
                // if (_cResourceCache.TryContentFileRead($"{rsi.RsiPath}/full.png", out var stream))
                // {
                //     image = Image.Load<Rgba32>(stream);
                // }
                // else
                // {
                //     image = new Image<Rgba32>(64, 64);
                //
                //     foreach (var specifier in wall.Specifiers)
                //     {
                //         switch (specifier)
                //         {
                //             case Rsi specifierRsi:
                //             {
                //                 if (!_cResourceCache.TryContentFileRead($"{specifierRsi.RsiPath}/{specifierRsi.RsiState}.png", out var specifierStream))
                //                 {
                //                     continue;
                //                 }
                //
                //                 var specifierImage = Image.Load<Rgba32>(specifierStream);
                //
                //                 image.Mutate(o => o.DrawImage(specifierImage, new Point(0, 0), 1));
                //
                //                 break;
                //             }
                //         }
                //     }
                // }


                // foreach (var specifier in wall.Specifiers)
                // {
                //     switch (specifier)
                //     {
                //         case Rsi rsi:
                //         {
                //             if (!_cResourceCache.TryContentFileRead($"{rsi.RsiPath}/full.png"))
                //             {
                //                 continue;
                //             }
                //             using var stream = _cResourceCache.ContentFileRead($"{rsi.RsiPath}/full.png");
                //
                //             image = Image.Load<Rgba32>(stream);
                //             break;
                //         }
                //         case Texture texture:
                //         {
                //             using var stream = _cResourceCache.ContentFileRead(texture.TexturePath);
                //
                //             image = Image.Load<Rgba32>(stream);
                //             break;
                //         }
                //         default:
                //             throw new ArgumentOutOfRangeException(nameof(specifier));
                //     }
                // }


                // image.Mutate(o => o.Resize(new ResizeOptions
                // {
                //     Mode = ResizeMode.Stretch,
                //     Size = new Size(32, 32)
                // }));
                //
                // gridCanvas.Mutate(o => o.DrawImage(image, new Point((wall.X + xOffset) * 32, (wall.Y + yOffset) * 32), 1));
            }

            Console.WriteLine($"{nameof(EntityPainter)} painted {walls.Count} walls on grid {grid.Index} in {(int) stopwatch.Elapsed.TotalMilliseconds} ms");
        }

        private ConcurrentDictionary<GridId, List<EntityData>> GetEntities()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var components = new ConcurrentDictionary<GridId, List<EntityData>>();

            foreach (var entity in _sEntityManager.GetEntities())
            {
                if (!entity.HasComponent<ISpriteRenderableComponent>())
                {
                    continue;
                }

                if (entity.Prototype == null)
                {
                    continue;
                }

                var clientEntity = _cEntityManager.GetEntity(entity.Uid);

                if (!clientEntity.TryGetComponent(out SpriteComponent sprite))
                {
                    throw new InvalidOperationException(
                        $"No sprite component found on an entity for which a server sprite component exists. Prototype id: {entity.Prototype?.ID}");
                }

                // var specifiers = new List<SpriteSpecifier>();
                //
                // if (clientEntity.TryGetComponent(out AppearanceComponent appearance))
                // {
                //     foreach (var layer in appearance.Visualizers)
                //     {
                //         layer.OnChangeData(appearance);
                //     }
                // }
                //
                // foreach (var layer in sprite.AllLayers)
                // {
                //     if (layer.Texture != null)
                //     {
                //         var specifier = _textures[layer.Texture];
                //         specifiers.Add(specifier);
                //     }
                //
                //     if (!layer.RsiState.IsValid || !layer.Visible)
                //     {
                //         continue;
                //     }
                //
                //     var rsi = layer.Rsi ?? sprite.BaseRSI;
                //     if (rsi != null &&
                //         rsi.TryGetState(layer.RsiState, out var state))
                //     {
                //         var specifier = new Rsi(rsi.Path, layer.RsiState.Name);
                //         specifiers.Add(specifier);
                //     }
                // }



                // foreach (var layer in sprite.AllLayers)
                // {
                //     var texture = layer.Texture;
                //
                //     if (texture != null)
                //     {
                //
                //         specifiers = new List<SpriteSpecifier> {new SpriteSpecifier.Texture(texture.)};
                //     }
                //
                //     var rsi = layer.ActualRsi;
                //
                //     if (rsi != null && spri != null)
                //     {
                //         specifiers = new List<SpriteSpecifier>
                //             {new Rsi(new ResourcePath(sprite.BaseRSIPath), sprite.State)};
                //     }
                //
                //     if (sprite.Texture != null)
                //     {
                //     }
                //
                //     var state = layer.RsiState;
                // }

                var position = entity.Transform.WorldPosition;
                var x = (int) Math.Floor(position.X);
                var y = (int) Math.Floor(position.Y);
                var data = new EntityData(sprite, x, y);

                components.GetOrAdd(entity.Transform.GridID, _ => new List<EntityData>()).Add(data);
            }

            Console.WriteLine($"Found {components.Values.Sum(l => l.Count)} walls on {components.Count} grids in {(int) stopwatch.Elapsed.TotalMilliseconds} ms");

            return components;
        }
    }
}
