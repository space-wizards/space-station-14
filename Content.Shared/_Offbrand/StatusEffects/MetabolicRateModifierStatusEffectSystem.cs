using Content.Shared._Offbrand.Wounds;
using Content.Shared.StatusEffectNew;

namespace Content.Shared._Offbrand.StatusEffects;

public sealed class MetabolicRateModifierStatusEffectSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MetabolicRateModifierStatusEffectComponent, StatusEffectRelayedEvent<ModifiedMetabolicRateEvent>>(OnModifiedMetabolicRate);
    }

    private void OnModifiedMetabolicRate(Entity<MetabolicRateModifierStatusEffectComponent> ent, ref StatusEffectRelayedEvent<ModifiedMetabolicRateEvent> args)
    {
        args.Args = args.Args with { Rate = Math.Clamp(args.Args.Rate + ent.Comp.Delta, ent.Comp.Min, ent.Comp.Max) };
    }
}
