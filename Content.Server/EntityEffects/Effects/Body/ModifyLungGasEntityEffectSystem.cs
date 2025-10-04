using Content.Server.Body.Components;
using Content.Shared.Atmos;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Body;

namespace Content.Server.EntityEffects.Effects.Body;

public sealed partial class ModifyLungGasEntityEffectSystem : EntityEffectSystem<LungComponent, ModifyLungGas>
{
    // TODO: This shouldn't be an entity effect, gasses should just metabolize and make a byproduct by default...
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
