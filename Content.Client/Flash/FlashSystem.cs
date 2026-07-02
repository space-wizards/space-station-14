using Content.Shared.Flash;
using Content.Shared.Flash.Components;
using Content.Shared.StatusEffectNew;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.Flash;

public sealed partial class FlashSystem : SharedFlashSystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IOverlayManager _overlayMan = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    private FlashOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FlashedStatusEffectComponent, StatusEffectAppliedEvent>(OnApplied);
        SubscribeLocalEvent<FlashedStatusEffectComponent, StatusEffectRemovedEvent>(OnRemoved);
        SubscribeLocalEvent<FlashedStatusEffectComponent, StatusEffectRelayedEvent<LocalPlayerAttachedEvent>>(OnPlayerAttached);
        SubscribeLocalEvent<FlashedStatusEffectComponent, StatusEffectRelayedEvent<LocalPlayerDetachedEvent>>(OnPlayerDetached);

        _overlay = new();
    }

    private void OnApplied(Entity<FlashedStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (_player.LocalEntity == args.Target)
        {
            _overlay.RequestScreenTexture = true;
            _overlayMan.AddOverlay(_overlay);
        }
    }

    private void OnRemoved(Entity<FlashedStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (_player.LocalEntity != args.Target)
            return;

        if (!_statusEffects.HasEffectComp<FlashedStatusEffectComponent>(args.Target))
        {
            _overlay.ScreenshotTexture = null;
            _overlay.RequestScreenTexture = false;
            _overlayMan.RemoveOverlay(_overlay);
        }
    }

    private void OnPlayerAttached(Entity<FlashedStatusEffectComponent> ent, ref StatusEffectRelayedEvent<LocalPlayerAttachedEvent> args)
    {
        _overlay.RequestScreenTexture = true;
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(Entity<FlashedStatusEffectComponent> ent, ref StatusEffectRelayedEvent<LocalPlayerDetachedEvent> args)
    {
        if (_player.LocalEntity is null || _statusEffects.HasEffectComp<FlashedStatusEffectComponent>(_player.LocalEntity.Value))
            return;

        _overlay.ScreenshotTexture = null;
        _overlay.RequestScreenTexture = false;
        _overlayMan.RemoveOverlay(_overlay);
    }
}
