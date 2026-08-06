using Content.Client.Graphics;
using Content.Shared.NodeCrawl;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;

namespace Content.Client.NodeCrawl;

public sealed class NodeCrawlPipeOverlay : Overlay
{
    private readonly NodeCrawlSystem _nodeCrawl;
    private readonly NodeCrawlCrawlerSystem _crawler;
    private readonly SpriteSystem _spriteSystem;
    private readonly EntityLookupSystem _lookup;
    private readonly SharedTransformSystem _transform;
    private readonly IPlayerManager _playerManager;
    private ShaderInstance _outlineShader;

    private readonly EntityQuery<NodeCrawlerMovementComponent> _movementQuery;
    private readonly EntityQuery<SpriteComponent> _spriteQuery;
    private readonly EntityQuery<EyeComponent> _eyeQuery;
    private readonly EntityQuery<TransformComponent> _transformQuery;
    private readonly HashSet<EntityUid> _entities = [];

    private static readonly Color NodeBaseColor = new(1f, 1f, 1f, 0.5f);
    private EntityUid? _previousOutlined;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public NodeCrawlPipeOverlay(IEntityManager entityManager, NodeCrawlSystem nodeCrawl, ShaderInstance outlineShader)
    {
        _nodeCrawl = nodeCrawl;
        _outlineShader = outlineShader;
        _movementQuery = entityManager.GetEntityQuery<NodeCrawlerMovementComponent>();
        _spriteQuery = entityManager.GetEntityQuery<SpriteComponent>();
        _eyeQuery = entityManager.GetEntityQuery<EyeComponent>();
        _transformQuery = entityManager.GetEntityQuery<TransformComponent>();
        _crawler = entityManager.System<NodeCrawlCrawlerSystem>();
        _spriteSystem = entityManager.System<SpriteSystem>();
        _lookup = entityManager.System<EntityLookupSystem>();
        _transform = entityManager.System<SharedTransformSystem>();
        _playerManager = IoCManager.Resolve<IPlayerManager>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var reachable = _nodeCrawl.ReachableNodes;
        var player = _playerManager.LocalSession?.AttachedEntity;
        if (reachable == null || player == null)
            return;

        EntityUid? current = null;
        if (_crawler.TryGetNodeCrawler(player.Value, out var crawler) &&
            crawler.Comp.Mover is { } mover &&
            _movementQuery.TryGetComponent(mover, out var movement))
        {
            current = movement.Node;
        }

        _entities.Clear();
        _lookup.GetEntitiesIntersecting(args.MapId, args.WorldBounds.CalcBoundingBox(), _entities, LookupFlags.Uncontained);
        var worldHandle = args.WorldHandle;
        var eyeRotation = _eyeQuery.TryGetComponent(player, out var eye)
            ? eye.Rotation
            : Angle.Zero;

        worldHandle.UseShader(null);
        foreach (var uid in _entities)
        {
            if (!reachable.Contains(uid) || !_spriteQuery.TryGetComponent(uid, out var sprite) || !sprite.Visible)
                continue;

            var (worldPos, worldRot) = _transform.GetWorldPositionRotation(_transformQuery.GetComponent(uid), _transformQuery);
            var oldColor = sprite.Color;

            _spriteSystem.SetColor((uid, sprite), NodeBaseColor);
            _spriteSystem.RenderSprite((uid, sprite), worldHandle, eyeRotation, worldRot, worldPos);
            _spriteSystem.SetColor((uid, sprite), oldColor);
        }

        SetOutline(current);
    }

    public void SetShader(ShaderInstance shader)
    {
        var outlined = _previousOutlined;
        RemoveOutline();
        _outlineShader = shader;
        SetOutline(outlined);
    }

    public void RemoveOutline()
    {
        SetOutline(null);
    }

    private void SetOutline(EntityUid? current)
    {
        if (_previousOutlined == current)
            return;

        if (_previousOutlined is { } previous && _spriteQuery.TryGetComponent(previous, out var previousSprite))
            _spriteSystem.RemovePostShader((previous, previousSprite), ContentPostShaderIds.NodeCrawlOutline);

        _previousOutlined = null;

        if (current is not { } uid || !_spriteQuery.TryGetComponent(uid, out var currentSprite))
            return;

        _spriteSystem.SetPostShader((uid, currentSprite),
            new SpriteComponent.PostShaderArgs(
            ContentPostShaderIds.NodeCrawlOutline,
            _outlineShader)
        {
            After = ContentPostShaderIds.AfterBaseEffects,
        });
        _previousOutlined = uid;
    }
}
