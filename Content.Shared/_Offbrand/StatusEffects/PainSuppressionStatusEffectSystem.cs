using Content.Shared._Offbrand.Wounds;
using Content.Shared.StatusEffectNew;

namespace Content.Shared._Offbrand.StatusEffects;

public sealed class PainSuppressionStatusEffectSystem : EntitySystem
{
    [Dependency] private readonly PainSystem _pain = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PainSuppressionStatusEffectComponent, StatusEffectAppliedEvent>(OnStatusEffectApplied);
        SubscribeLocalEvent<PainSuppressionStatusEffectComponent, StatusEffectRemovedEvent>(OnStatusEffectRemoved);
        SubscribeLocalEvent<PainSuppressionStatusEffectComponent, StatusEffectRelayedEvent<PainSuppressionEvent>>(OnPainSuppression);
    }

    private void OnPainSuppression(Entity<PainSuppressionStatusEffectComponent> ent, ref StatusEffectRelayedEvent<PainSuppressionEvent> args)
    {
        args.Args = args.Args with { Suppressed = true };
    }

    private void OnStatusEffectApplied(Entity<PainSuppressionStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        _pain.UpdateSuppression(args.Target);
    }

    private void OnStatusEffectRemoved(Entity<PainSuppressionStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        _pain.UpdateSuppression(args.Target);
    }
}
