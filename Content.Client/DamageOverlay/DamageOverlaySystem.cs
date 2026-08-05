using Content.Shared.DamageOverlay;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.DamageOverlay;

public sealed partial class DamageOverlaySystem : SharedDamageOverlaySystem
{
    [Dependency] private IOverlayManager _overlayManager = default!;
    [Dependency] private IPlayerManager _player = default!;

    private DamageOverlay _overlay = default!;

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
        RefreshOverlay(ent);
    }

    [SubscribeLocalEvent]
    public void OnPlayerDetached(LocalPlayerDetachedEvent args)
    {
        _overlayManager.RemoveOverlay(_overlay);
    }

    [SubscribeLocalEvent]
    public void OnAfterState(Entity<DamageOverlayComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RefreshOverlay(ent);
    }

    protected override void RefreshOverlay(Entity<DamageOverlayComponent> entity)
    {
        base.RefreshOverlay(entity);

        if (_player.LocalEntity != entity)
            return;

        if (!_overlayManager.HasOverlay<DamageOverlay>())
            _overlayManager.AddOverlay(_overlay);

        _overlay.State = entity.Comp.State;
        _overlay.CritLevel = entity.Comp.CritLevel;
        _overlay.DeadLevel = entity.Comp.DeadLevel;
        _overlay.OxygenLevel = entity.Comp.OxygenLevel;
        _overlay.PainLevel = entity.Comp.PainLevel;
    }
}
