using Content.Shared.Drugs;
using Content.Shared.StatusEffectNew;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Client.Drugs;

/// <summary>
///     System to handle drug related overlays.
/// </summary>
public sealed partial class DrugOverlaySystem : EntitySystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IOverlayManager _overlayMan = default!;
    [Dependency] private IRobustRandom _random = default!;

    private RainbowOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new();
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<SeeingRainbowsStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (_player.LocalEntity != args.Target)
            return;

        _overlay.Intoxication = 0;
        _overlay.TimeTicker = 0;
        _overlayMan.RemoveOverlay(_overlay);
    }

    [SubscribeLocalEvent]
    private void OnApplied(Entity<SeeingRainbowsStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (_player.LocalEntity != args.Target)
            return;

        _overlay.Phase = _random.NextFloat(MathF.Tau); // random starting phase for movement effect
        _overlayMan.AddOverlay(_overlay);
    }

    [SubscribeLocalEvent]
    private void OnPlayerAttached(Entity<SeeingRainbowsStatusEffectComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        _overlayMan.AddOverlay(_overlay);
    }

    [SubscribeLocalEvent]
    private void OnPlayerDetached(Entity<SeeingRainbowsStatusEffectComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        _overlay.Intoxication = 0;
        _overlay.TimeTicker = 0;
        _overlayMan.RemoveOverlay(_overlay);
    }
}
