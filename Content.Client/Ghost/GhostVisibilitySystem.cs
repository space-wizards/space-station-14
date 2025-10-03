using Content.Shared.Ghost;
using Robust.Client.GameObjects;
using Robust.Shared.Player;

namespace Content.Client.Ghost;

public sealed class GhostVisibilitySystem : SharedGhostVisibilitySystem
{
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private bool _showGhosts;

    /// <summary>
    /// Whether hidden/invisible ghost sprites should be drawn.
    /// </summary>
    /// <remarks>
    /// This can be used to toggle drawing other spectator ghosts. However, if the ghost is actually visible for
    /// everyone they will still get drawn regardless.
    /// </remarks>
    public bool ShowGhosts
    {
        get => _showGhosts;
        set
        {
            if (_showGhosts == value)
            {
                return;
            }

            _showGhosts = value;
            var query = AllEntityQuery<GhostVisibilityComponent, SpriteComponent>();
            while (query.MoveNext(out var uid, out var ghost, out var sprite))
            {
                UpdateSpriteVisibility((uid, ghost, sprite));
            }
        }
    }

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GhostVisibilityComponent, AfterAutoHandleStateEvent>(OnGhostVisState);
        SubscribeLocalEvent<GhostVisibilityComponent, PlayerAttachedEvent>(OnAttached);
        SubscribeLocalEvent<GhostVisibilityComponent, PlayerDetachedEvent>(OnDetached);
    }

    private void OnDetached(Entity<GhostVisibilityComponent> ent, ref PlayerDetachedEvent args)
    {
        UpdateVisibility(ent.AsNullable());
    }

    private void OnAttached(Entity<GhostVisibilityComponent> ent, ref PlayerAttachedEvent args)
    {
        UpdateVisibility(ent.AsNullable());
    }

    private void OnGhostVisState(EntityUid uid, GhostVisibilityComponent component, ref AfterAutoHandleStateEvent args)
    {
        UpdateSpriteVisibility((uid, component));
    }

    protected override void UpdateVisibility(Entity<GhostVisibilityComponent?, VisibilityComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp1))
            return;

        // Intentionally not calling the base event / modifying component data.
        // Client cannot predict ghost visibility due to lack of round-end information, and because the global
        // visibility is not networked.

        UpdateSpriteVisibility((ent.Owner, ent.Comp1));
    }

    private void UpdateSpriteVisibility(Entity<GhostVisibilityComponent?, SpriteComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp1, ref ent.Comp2))
            return;

        var visible = ShowGhosts || ent.Comp1.Visible || ent.Owner == _player.LocalEntity;
        _sprite.SetVisible((ent.Owner, ent.Comp2), visible);
    }
}
