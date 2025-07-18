using Content.Shared.Drowsiness;
using Content.Shared.StatusEffectNew;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.Drowsiness;

public sealed class DrowsinessSystem : SharedDrowsinessSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly SharedStatusEffectsSystem _statusEffects = default!;

    private DrowsinessOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DrowsinessStatusEffectComponent, StatusEffectAppliedEvent>(OnDrowsinessApply);
        SubscribeLocalEvent<DrowsinessStatusEffectComponent, StatusEffectRemovedEvent>(OnDrowsinessShutdown);

        SubscribeLocalEvent<DrowsinessStatusEffectComponent, StatusEffectRelayedEvent<LocalPlayerAttachedEvent>>(OnStatusEffectPlayerAttached);
        SubscribeLocalEvent<DrowsinessStatusEffectComponent, StatusEffectRelayedEvent<LocalPlayerDetachedEvent>>(OnStatusEffectPlayerDetached);

        _overlay = new();
    }

    private void OnDrowsinessApply(Entity<DrowsinessStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (_player.LocalEntity == args.Target)
            _overlayMan.AddOverlay(_overlay);
    }

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

    private void OnStatusEffectPlayerAttached(Entity<DrowsinessStatusEffectComponent> ent, ref StatusEffectRelayedEvent<LocalPlayerAttachedEvent> args)
    {
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnStatusEffectPlayerDetached(Entity<DrowsinessStatusEffectComponent> ent, ref StatusEffectRelayedEvent<LocalPlayerDetachedEvent> args)
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
