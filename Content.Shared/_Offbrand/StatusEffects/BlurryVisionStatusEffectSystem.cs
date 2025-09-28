using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.StatusEffectNew;

namespace Content.Shared._Offbrand.StatusEffects;

public sealed class BlurryVisionStatusEffectSystem : EntitySystem
{
    [Dependency] private readonly BlurryVisionSystem _blurryVision = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlurryVisionStatusEffectComponent, StatusEffectAppliedEvent>(OnStatusEffectApplied);
        SubscribeLocalEvent<BlurryVisionStatusEffectComponent, StatusEffectRemovedEvent>(OnStatusEffectRemoved);
        SubscribeLocalEvent<BlurryVisionStatusEffectComponent, StatusEffectRelayedEvent<GetBlurEvent>>(OnGetBlur);
    }

    private void OnStatusEffectApplied(Entity<BlurryVisionStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        _blurryVision.UpdateBlurMagnitude(args.Target);
    }

    private void OnStatusEffectRemoved(Entity<BlurryVisionStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        _blurryVision.UpdateBlurMagnitude(args.Target);
    }

    private void OnGetBlur(Entity<BlurryVisionStatusEffectComponent> ent, ref StatusEffectRelayedEvent<GetBlurEvent> args)
    {
        args.Args.Blur += ent.Comp.Blur;
        args.Args.CorrectionPower *= ent.Comp.CorrectionPower;
    }
}
