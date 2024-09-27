using System.Numerics;
using Content.Shared.Silicons.StationAi;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Collections;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Silicons.StationAi;

public sealed class StationAiOverlay : Overlay
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly HashSet<Vector2i> _visibleTiles = new();

    private IRenderTexture? _staticTexture;
    private IRenderTexture? _stencilTexture;

    private float _updateRate = 1f / 30f;
    private float _accumulator;

    public StationAiOverlay()
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_stencilTexture?.Texture.Size != args.Viewport.Size)
        {
            _staticTexture?.Dispose();
            _stencilTexture?.Dispose();
            _stencilTexture = _clyde.CreateRenderTarget(args.Viewport.Size, new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), name: "station-ai-stencil");
            _staticTexture = _clyde.CreateRenderTarget(args.Viewport.Size,
                new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb),
                name: "station-ai-static");
        }

        var worldHandle = args.WorldHandle;

        var worldBounds = args.WorldBounds;

        var playerEnt = _player.LocalEntity;
        _entManager.TryGetComponent(playerEnt, out TransformComponent? playerXform);
        var gridUid = playerXform?.GridUid ?? EntityUid.Invalid;
        _entManager.TryGetComponent(gridUid, out MapGridComponent? grid);
        _entManager.TryGetComponent(gridUid, out BroadphaseComponent? broadphase);

        var invMatrix = args.Viewport.GetWorldToLocalMatrix();
        _accumulator -= (float) _timing.FrameTime.TotalSeconds;

        if (grid != null && broadphase != null)
        {
            var maps = _entManager.System<SharedMapSystem>();
            var lookups = _entManager.System<EntityLookupSystem>();
            var xforms = _entManager.System<SharedTransformSystem>();
            var vision = _entManager.System<StationAiVisionSystem>();

            if (_accumulator <= 0f)
            {
                _visibleTiles.Clear();
                vision.GetView((gridUid, broadphase, grid), worldBounds.Enlarged(1f), _visibleTiles, new HashSet<Vector2i>());
            }

            var gridMatrix = xforms.GetWorldMatrix(gridUid);
            var matty =  Matrix3x2.Multiply(gridMatrix, invMatrix);

            // Draw visible tiles to stencil
            worldHandle.RenderInRenderTarget(_stencilTexture!, () =>
            {
                worldHandle.SetTransform(matty);

                foreach (var tile in _visibleTiles)
                {
                    var aabb = lookups.GetLocalBounds(tile, grid.TileSize);
                    worldHandle.DrawRect(aabb, Color.White);
                }
            },
            Color.Transparent);

            // Once this is gucci optimise rendering.
            worldHandle.RenderInRenderTarget(_staticTexture!,
            () =>
            {
                worldHandle.SetTransform(matty);

                var tiles = maps.GetTilesEnumerator(gridUid, grid, worldBounds.Enlarged(grid.TileSize / 2f));
                var gridEnt = new Entity<BroadphaseComponent, MapGridComponent>(gridUid, _entManager.GetComponent<BroadphaseComponent>(gridUid), grid);
                var airlockVertCache = new ValueList<Vector2>(9);
                var airlockColor = Color.Gold;
                var airlockVerts = new ValueList<Vector2>();

                while (tiles.MoveNext(out var tileRef))
                {
                    if (_visibleTiles.Contains(tileRef.GridIndices))
                        continue;

                    // TODO: GetView should do these.
                    var aabb = lookups.GetLocalBounds(tileRef.GridIndices, grid.TileSize);

                    if (vision.TryAirlock(gridEnt, tileRef.GridIndices, out var open))
                    {
                        var midBottom = (aabb.BottomRight - aabb.BottomLeft) / 2f + aabb.BottomLeft;
                        var midTop = (aabb.TopRight - aabb.TopLeft) / 2f + aabb.TopLeft;
                        const float IndentSize = 0.10f;
                        const float OpenOffset = 0.25f;

                        // Use triangle-fan and draw from the mid-vert

                        // Left half
                        {
                            airlockVertCache.Clear();
                            airlockVertCache.Add(aabb.Center with { X = aabb.Center.X - aabb.Width / 2f });
                            airlockVertCache.Add(aabb.BottomLeft);
                            airlockVertCache.Add(midBottom);
                            airlockVertCache.Add(airlockVertCache[^1] + new Vector2(0f, grid.TileSize * 0.35f));
                            airlockVertCache.Add(airlockVertCache[^1] + new Vector2(-grid.TileSize * IndentSize, grid.TileSize * 0.15f));
                            airlockVertCache.Add(airlockVertCache[^1] + new Vector2(grid.TileSize * IndentSize, grid.TileSize * 0.15f));
                            airlockVertCache.Add(midTop);
                            airlockVertCache.Add(aabb.TopLeft);

                            if (open)
                            {
                                for (var i = 0; i < 8; i++)
                                {
                                    airlockVertCache[i] -= new Vector2(OpenOffset, 0f);
                                }
                            }

                            for (var i = 0; i < airlockVertCache.Count; i++)
                            {
                                airlockVerts.Add(airlockVertCache[i]);
                                var next = (airlockVertCache[(i + 1) % airlockVertCache.Count]);
                                airlockVerts.Add(next);
                            }

                            worldHandle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, airlockVertCache.Span, airlockColor.WithAlpha(0.05f));
                        }

                        // Right half
                        {
                            airlockVertCache.Clear();
                            airlockVertCache.Add(aabb.Center with { X = aabb.Center.X + aabb.Width / 2f });
                            airlockVertCache.Add(aabb.BottomRight);
                            airlockVertCache.Add(midBottom);
                            airlockVertCache.Add(airlockVertCache[^1] + new Vector2(0f, grid.TileSize * 0.35f));
                            airlockVertCache.Add(airlockVertCache[^1] + new Vector2(grid.TileSize * IndentSize, 0.15f));
                            airlockVertCache.Add(airlockVertCache[^1] + new Vector2(-grid.TileSize * IndentSize, grid.TileSize * 0.15f));
                            airlockVertCache.Add(midTop);
                            airlockVertCache.Add(aabb.TopRight);

                            if (open)
                            {
                                for (var i = 0; i < 8; i++)
                                {
                                    airlockVertCache[i] += new Vector2(OpenOffset, 0f);
                                }
                            }

                            for (var i = 0; i < airlockVertCache.Count; i++)
                            {
                                airlockVerts.Add(airlockVertCache[i]);
                                var next = (airlockVertCache[(i + 1) % airlockVertCache.Count]);
                                airlockVerts.Add(next);
                            }

                            worldHandle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, airlockVertCache.Span, airlockColor.WithAlpha(0.05f));
                        }

                        continue;
                    }


                    var occluded = vision.IsOccluded(gridEnt, tileRef.GridIndices);

                    // Draw walls
                    if (occluded)
                    {
                        worldHandle.DrawRect(aabb, Color.LimeGreen.WithAlpha(0.05f));
                        worldHandle.DrawRect(aabb, Color.LimeGreen, filled: false);
                    }
                    // Draw tiles
                    else
                    {
                        worldHandle.DrawRect(aabb, Color.Green.WithAlpha(0.35f), filled: false);
                    }
                }

                worldHandle.DrawPrimitives(DrawPrimitiveTopology.LineList, airlockVerts.Span, Color.Gold);
            },
            Color.Black);
        }
        // Not on a grid
        else
        {
            worldHandle.RenderInRenderTarget(_stencilTexture!, () =>
            {
            },
            Color.Transparent);

            worldHandle.RenderInRenderTarget(_staticTexture!,
            () =>
            {
                worldHandle.SetTransform(Matrix3x2.Identity);
                worldHandle.DrawRect(worldBounds, Color.Black);
            }, Color.Black);
        }

        if (_accumulator <= 0f)
        {
            _accumulator = MathF.Max(0f, _accumulator + _updateRate);
        }

        // Use the lighting as a mask
        worldHandle.UseShader(_proto.Index<ShaderPrototype>("StencilMask").Instance());
        worldHandle.DrawTextureRect(_stencilTexture!.Texture, worldBounds);

        // Draw the static
        worldHandle.UseShader(_proto.Index<ShaderPrototype>("StencilDraw").Instance());
        worldHandle.DrawTextureRect(_staticTexture!.Texture, worldBounds);

        worldHandle.SetTransform(Matrix3x2.Identity);
        worldHandle.UseShader(null);

    }
}
