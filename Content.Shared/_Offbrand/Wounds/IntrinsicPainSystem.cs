using Content.Shared.Damage;

namespace Content.Shared._Offbrand.Wounds;

public sealed class IntrinsicPainSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IntrinsicPainComponent, GetPainEvent>(OnGetPain);
    }

    private void OnGetPain(Entity<IntrinsicPainComponent> ent, ref GetPainEvent args)
    {
        var damageable = Comp<DamageableComponent>(ent);

        foreach (var (type, coefficient) in ent.Comp.PainCoefficients)
        {
            if (damageable.Damage.DamageDict.TryGetValue(type, out var damage))
            {
                args.Pain += coefficient * damage;
            }
        }
    }
}
