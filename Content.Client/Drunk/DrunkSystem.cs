using Content.Shared.Drunk;
using Content.Shared.StatusEffectNew;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Client.Drunk;

public sealed partial class DrunkSystem : SharedDrunkSystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IOverlayManager _overlayMan = default!;
    [Dependency] private IRobustRandom _random = default!;

    private DrunkOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new();
    }

    [SubscribeLocalEvent]
    private void OnStatusApplied(Entity<DrunkStatusEffectComponent> entity, ref StatusEffectAppliedEvent args)
    {
        if (!_overlayMan.HasOverlay<DrunkOverlay>())
        {
            _overlay.Phase = _random.NextFloat(MathF.Tau); // random starting phase for movement effect
            _overlayMan.AddOverlay(_overlay);
        }
    }

    [SubscribeLocalEvent]
    private void OnStatusRemoved(Entity<DrunkStatusEffectComponent> entity, ref StatusEffectRemovedEvent args)
    {
        if (Status.HasEffectComp<DrunkStatusEffectComponent>(args.Target))
            return;

        if (_player.LocalEntity != args.Target)
            return;

        _overlay.CurrentBoozePower = 0;
        _overlayMan.RemoveOverlay(_overlay);
    }

    [SubscribeLocalEvent]
    private void OnPlayerAttached(Entity<DrunkStatusEffectComponent> entity, ref LocalPlayerAttachedEvent args)
    {
        _overlayMan.AddOverlay(_overlay);
    }

    [SubscribeLocalEvent]
    private void OnPlayerDetached(Entity<DrunkStatusEffectComponent> entity, ref LocalPlayerDetachedEvent args)
    {
        _overlay.CurrentBoozePower = 0;
        _overlayMan.RemoveOverlay(_overlay);
    }
}
