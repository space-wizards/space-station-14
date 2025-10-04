using Content.Shared._Offbrand.Wounds;
using Content.Shared.StatusEffectNew;

namespace Content.Shared._Offbrand.StatusEffects;

public sealed class StrainStatusEffectSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StrainStatusEffectComponent, StatusEffectRelayedEvent<GetStrainEvent>>(OnGetStrain);
    }

    private void OnGetStrain(Entity<StrainStatusEffectComponent> ent, ref StatusEffectRelayedEvent<GetStrainEvent> args)
    {
        args.Args = args.Args with { Strain = args.Args.Strain + ent.Comp.Delta };
    }
}
