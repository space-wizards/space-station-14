using Content.Server.Body.Components;
using Content.Shared.Atmos;
using Content.Shared.EntityEffects;

namespace Content.Server.EntityEffects.Effects.Body;

public sealed partial class ModifyLungGasEntityEffectSystem : EntityEffectSystem<LungComponent, ModifyLungGas>
{
    // TODO: KILL KILL KILL KILL KILL KILL KILL KILL KILL KILL KILL KILL KILL KILL KILL
    // TODO: It might be time to bite the bullet and implement gas entity effects...
    protected override void Effect(Entity<LungComponent> entity, ref EntityEffectEvent<ModifyLungGas> args)
    {
        var amount = args.Scale;

        foreach (var (gas, ratio) in args.Effect.Ratios)
        {
            var quantity = ratio * amount / Atmospherics.BreathMolesToReagentMultiplier;
            if (quantity < 0)
                quantity = Math.Max(quantity, -entity.Comp.Air[(int) gas]);
            entity.Comp.Air.AdjustMoles(gas, quantity);
        }
    }
}

public sealed class ModifyLungGas : EntityEffectBase<ModifyLungGas>
{
    [DataField(required: true)]
    public Dictionary<Gas, float> Ratios = default!;
}
