using Content.Shared.Drowsiness;
using Content.Shared.StatusEffectNew;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.Drowsiness;

public sealed partial class DrowsinessSystem : SharedDrowsinessSystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IOverlayManager _overlayMan = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    private DrowsinessOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new();
    }

    [SubscribeLocalEvent]
    private void OnDrowsinessApply(Entity<DrowsinessStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (_player.LocalEntity == args.Target)
            _overlayMan.AddOverlay(_overlay);
    }

    [SubscribeLocalEvent]
    private void OnDrowsinessShutdown(Entity<DrowsinessStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (_player.LocalEntity != args.Target)
            return;

        if (!_statusEffects.HasEffectComp<DrowsinessStatusEffectComponent>(_player.LocalEntity.Value))
        {
            _overlay.CurrentPower = 0;
            _overlayMan.RemoveOverlay(_overlay);
        }
    }

    [SubscribeLocalEvent]
    private void OnStatusEffectPlayerAttached(Entity<DrowsinessStatusEffectComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        _overlayMan.AddOverlay(_overlay);
    }

    [SubscribeLocalEvent]
    private void OnStatusEffectPlayerDetached(Entity<DrowsinessStatusEffectComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        if (_player.LocalEntity is null)
            return;

        if (!_statusEffects.HasEffectComp<DrowsinessStatusEffectComponent>(_player.LocalEntity.Value))
        {
            _overlay.CurrentPower = 0;
            _overlayMan.RemoveOverlay(_overlay);
        }
    }
}
