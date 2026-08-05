using Content.Client.Graphics;
using Content.Shared.NodeCrawl;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;

namespace Content.Client.NodeCrawl;

public sealed class NodeCrawlPipeOverlay : Overlay
{
    private readonly IEntityManager _entityManager;
    private readonly NodeCrawlSystem _nodeCrawl;
    private readonly NodeCrawlCrawlerSystem _crawler;
    private readonly SpriteSystem _spriteSystem;
    private readonly EntityLookupSystem _lookup;
    private readonly SharedTransformSystem _transform;
    private readonly IPlayerManager _playerManager;
    private readonly ShaderInstance _outlineShader;

    private static readonly Color NodeBaseColor = new(1f, 1f, 1f, 0.5f);
    private EntityUid? _previousOutlined;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public NodeCrawlPipeOverlay(IEntityManager entityManager, NodeCrawlSystem nodeCrawl, ShaderInstance outlineShader)
    {
        _entityManager = entityManager;
        _nodeCrawl = nodeCrawl;
        _outlineShader = outlineShader;
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
            _entityManager.TryGetComponent<NodeCrawlerMovementComponent>(mover, out var movement))
        {
            current = movement.Node;
        }

        var entities = _lookup.GetEntitiesIntersecting(args.MapId, args.WorldBounds, LookupFlags.Uncontained);
        var worldHandle = args.WorldHandle;
        var eyeRotation = _entityManager.TryGetComponent<EyeComponent>(player, out var eye)
            ? eye.Rotation
            : Angle.Zero;

        worldHandle.UseShader(null);
        foreach (var uid in entities)
        {
            if (!reachable.Contains(uid) || !_entityManager.TryGetComponent<SpriteComponent>(uid, out var sprite) || !sprite.Visible)
                continue;

            var worldPos = _transform.GetWorldPosition(uid);
            var worldRot = _transform.GetWorldRotation(uid);
            var oldColor = sprite.Color;

            _spriteSystem.SetColor((uid, sprite), NodeBaseColor);
            _spriteSystem.RenderSprite((uid, sprite), worldHandle, eyeRotation, worldRot, worldPos);
            _spriteSystem.SetColor((uid, sprite), oldColor);
        }

        SetOutline(current);
    }

    public void RemoveOutline()
    {
        SetOutline(null);
    }

    private void SetOutline(EntityUid? current)
    {
        if (_previousOutlined == current)
            return;

        if (_previousOutlined is { } previous && _entityManager.TryGetComponent<SpriteComponent>(previous, out var previousSprite))
            _spriteSystem.RemovePostShader((previous, previousSprite), ContentPostShaderIds.NodeCrawlOutline);

        _previousOutlined = null;

        if (current is not { } uid || !_entityManager.TryGetComponent<SpriteComponent>(uid, out var currentSprite))
            return;

        _spriteSystem.SetPostShader((uid, currentSprite), new SpriteComponent.PostShaderArgs(
            ContentPostShaderIds.NodeCrawlOutline,
            _outlineShader)
        {
            After = ContentPostShaderIds.AfterBaseEffects,
        });
        _previousOutlined = uid;
    }
}
