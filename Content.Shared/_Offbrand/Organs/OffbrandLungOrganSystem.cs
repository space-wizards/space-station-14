using Content.Shared._Offbrand.Wounds;
using Content.Shared.Body;
using Content.Shared.FixedPoint;

namespace Content.Shared._Offbrand.Organs;

public sealed class OffbrandLungOrganSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OffbrandLungOrganComponent, BodyRelayedEvent<BeforeBreathEvent>>(OnBeforeBreath);
        SubscribeLocalEvent<OffbrandLungOrganComponent, BodyRelayedEvent<BaseLungFunctionEvent>>(OnBaseLungFunction);
    }

    private void OnBeforeBreath(Entity<OffbrandLungOrganComponent> ent, ref BodyRelayedEvent<BeforeBreathEvent> args)
    {
        var damage = Comp<DamageableOrganComponent>(ent);
        args.Args = args.Args with { BreathVolume = args.Args.BreathVolume * 1f - MathF.Pow(damage.Damage.Float() / damage.MaxDamage.Float(), 3f) };
    }

    private void OnBaseLungFunction(Entity<OffbrandLungOrganComponent> ent, ref BodyRelayedEvent<BaseLungFunctionEvent> args)
    {
        var damageComp = Comp<DamageableOrganComponent>(ent);
        var damage = damageComp.Damage.Float() / damageComp.MaxDamage.Float();
        var health = 1f - damage;
        var asphyxiationAmount = FixedPoint2.Zero;
        // var damageable = Comp<DamageableComponent>(ent);
        // if (!damageable.Damage.DamageDict.TryGetValue(ent.Comp.AsphyxiationDamage, out var asphyxiationAmount))
        // {
        //    args.Function *= health - damage;
        //    return;
        // }

        var airSupply = Math.Clamp(1f - (asphyxiationAmount.Float() / ent.Comp.AsphyxiationThreshold.Float()), 0, 1);

        args.Args = args.Args with { Function = health * airSupply };
    }
}
