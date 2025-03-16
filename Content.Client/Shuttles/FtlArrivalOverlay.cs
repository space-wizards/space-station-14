using System.Numerics;
using Content.Shared.Shuttles.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Shuttles;

/// <summary>
/// Plays a visualization whenever a shuttle is arriving from FTL.
/// </summary>
public sealed class FtlArrivalOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowEntities;

    private EntityLookupSystem _lookups;
    private SharedMapSystem _maps;
    private SharedTransformSystem _transforms;
    private SpriteSystem _sprites;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _protos = default!;

    private readonly HashSet<Entity<FtlVisualizerComponent>> _visualizers = new();

    private ShaderInstance _shader;

    public FtlArrivalOverlay()
    {
        IoCManager.InjectDependencies(this);
        _lookups = _entManager.System<EntityLookupSystem>();
        _transforms = _entManager.System<SharedTransformSystem>();
        _maps = _entManager.System<SharedMapSystem>();
        _sprites = _entManager.System<SpriteSystem>();

        _shader = _protos.Index<ShaderPrototype>("unshaded").Instance();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        _visualizers.Clear();
        _lookups.GetEntitiesOnMap(args.MapId, _visualizers);

        return _visualizers.Count > 0;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        args.WorldHandle.UseShader(_shader);

        foreach (var (uid, comp) in _visualizers)
        {
            var grid = comp.Grid;

            if (!_entManager.TryGetComponent(grid, out MapGridComponent? mapGrid))
                continue;

            var texture = _sprites.GetFrame(comp.Sprite, TimeSpan.FromSeconds(comp.Elapsed), loop: false);
            comp.Elapsed += (float) _timing.FrameTime.TotalSeconds;

            // Need to manually transform the viewport in terms of the visualizer entity as the grid isn't in position.
            var (_, _, worldMatrix, invMatrix) = _transforms.GetWorldPositionRotationMatrixWithInv(uid);
            args.WorldHandle.SetTransform(worldMatrix);
            var localAABB = invMatrix.TransformBox(args.WorldBounds);

            var tilesEnumerator = _maps.GetLocalTilesEnumerator(grid, mapGrid, localAABB);

            while (tilesEnumerator.MoveNext(out var tile))
            {
                var bounds = _lookups.GetLocalBounds(tile, mapGrid.TileSize);

                args.WorldHandle.DrawTextureRect(texture, bounds);
            }
        }

        args.WorldHandle.UseShader(null);
        args.WorldHandle.SetTransform(Matrix3x2.Identity);
    }
}
