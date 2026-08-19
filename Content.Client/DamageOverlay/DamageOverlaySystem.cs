using Content.Shared.DamageOverlay;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.DamageOverlay;

/// <inheritdoc />
public sealed partial class DamageOverlaySystem : SharedDamageOverlaySystem
{
    [Dependency] private IOverlayManager _overlayManager = default!;
    [Dependency] private IPlayerManager _player = default!;

    private DamageOverlay _overlay = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        _overlay = new DamageOverlay();
    }

    [SubscribeLocalEvent]
    private void OnComponentShutdown(Entity<DamageOverlayComponent> entity, ref ComponentShutdown args)
    {
        if (_player.LocalEntity != entity)
            return;

        _overlayManager.RemoveOverlay(_overlay);
    }

    [SubscribeLocalEvent]
    private void OnPlayerAttached(Entity<DamageOverlayComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        _overlayManager.AddOverlay(_overlay);
    }

    [SubscribeLocalEvent]
    private void OnPlayerDetached(LocalPlayerDetachedEvent args)
    {
        _overlayManager.RemoveOverlay(_overlay);
    }

    protected override void EnsureOverlay(Entity<DamageOverlayComponent> entity)
    {
        base.EnsureOverlay(entity);

        if (_player.LocalEntity != entity)
            return;

        if (!_overlayManager.HasOverlay<DamageOverlay>())
            _overlayManager.AddOverlay(_overlay);
    }
}
