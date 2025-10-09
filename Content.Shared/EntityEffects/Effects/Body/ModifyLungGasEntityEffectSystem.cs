using Content.Shared.Atmos;
using Content.Shared.Body.Components;

namespace Content.Shared.EntityEffects.Effects.Body;

/// <summary>
/// Adjust the amount of Moles stored in this set of lungs based on a given dictionary of gasses and ratios.
/// The amount of gas adjusted is modified by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
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

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class ModifyLungGas : EntityEffectBase<ModifyLungGas>
{
    [DataField(required: true)]
    public Dictionary<Gas, float> Ratios = default!;
}
