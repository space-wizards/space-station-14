using Content.Shared.Ghost;
using Robust.Client.GameObjects;
using Robust.Shared.Player;

namespace Content.Client.Ghost;

public sealed class GhostVisibilitySystem: SharedGhostVisibilitySystem
{
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    private bool _ghostVisibility;

    /// <summary>
    /// Whether ghost sprites should be visible or not.
    /// </summary>
    public bool GhostVisibility
    {
        get => _ghostVisibility;
        set
        {
            if (_ghostVisibility == value)
            {
                return;
            }

            _ghostVisibility = value;
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
    }

    private void OnGhostVisState(EntityUid uid, GhostVisibilityComponent component, ref AfterAutoHandleStateEvent args)
    {
        UpdateSpriteVisibility((uid, component));
    }

    public void ToggleGhostVisibility()
    {
        GhostVisibility = !GhostVisibility;
    }

    public override void UpdateVisibility(Entity<GhostVisibilityComponent?, VisibilityComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp1))
            return;

        base.UpdateVisibility(ent);
        UpdateSpriteVisibility((ent.Owner, ent.Comp1));
    }

    private void UpdateSpriteVisibility(Entity<GhostVisibilityComponent?, SpriteComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp1, ref ent.Comp2))
            return;

        ent.Comp2.Visible = GhostVisibility || ent.Comp1.Visible || ent.Owner == _player.LocalEntity;
    }
}
