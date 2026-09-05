using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;

namespace Content.Shared.EntityEffects.Effects.Damage;

public sealed partial class StaminaDamageEntityEffectSystem : EntityEffectSystem<StaminaComponent, StaminaDamage>
{
    [Dependency] private SharedStaminaSystem _stamina = default!;

    protected override void Effect(Entity<StaminaComponent> entity, ref EntityEffectEvent<StaminaDamage> args)
    {
        _stamina.TakeStaminaDamage(entity, args.Scale * args.Effect.Damage, entity.Comp, args.User);
    }
}

public sealed partial class StaminaDamage : EntityEffectBase<StaminaDamage>
{
    /// <summary>
    /// The amount of stamina damage we're dealing.
    /// </summary>
    [DataField(required: true)]
    public float Damage;
}

