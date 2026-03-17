using Content.Server.Botany.Components;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;
using Robust.Shared.Random;

namespace Content.Server.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// This system mutates an inputted stat for a PlantHolder, only works for floats, integers, and bools.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantChangeStatEntityEffectSystem : EntityEffectSystem<PlantHolderComponent, PlantChangeStat>
{
    // TODO: This is awful. I do not have the strength to refactor this. I want it gone.
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void Effect(Entity<PlantHolderComponent> entity, ref EntityEffectEvent<PlantChangeStat> args)
    {
        if (entity.Comp.Seed == null || entity.Comp.Dead)
            return;

        var effect = args.Effect;
        var member = entity.Comp.Seed.GetType().GetField(args.Effect.TargetValue);

        if (member == null)
        {
            Log.Error($"{ effect.GetType().Name } Error: Member { args.Effect.TargetValue} not found on { entity.Comp.Seed.GetType().Name }. Did you misspell it?");
            return;
        }

        var currentValObj = member.GetValue(entity.Comp.Seed);
        if (currentValObj == null)
            return;

        if (member.FieldType == typeof(float))
        {
            var floatVal = (float)currentValObj;
            MutateFloat(ref floatVal, args.Effect.MinValue, args.Effect.MaxValue, args.Effect.Steps);
            member.SetValue(entity.Comp.Seed, floatVal);
        }
        else if (member.FieldType == typeof(int))
        {
            var intVal = (int)currentValObj;
            MutateInt(ref intVal, (int)args.Effect.MinValue, (int)args.Effect.MaxValue, args.Effect.Steps);
            member.SetValue(entity.Comp.Seed, intVal);
        }
        else if (member.FieldType == typeof(bool))
        {
            var boolVal = (bool)currentValObj;
            boolVal = !boolVal;
            member.SetValue(entity.Comp.Seed, boolVal);
        }
    }

    // Mutate reference 'val' between 'min' and 'max' by pretending the value
    // is representable by a thermometer code with 'bits' number of bits and
    // randomly flipping some of them.
    private void MutateFloat(ref float val, float min, float max, int bits)
    {
        if (min == max)
        {
            val = min;
            return;
        }

        // Starting number of bits that are high, between 0 and bits.
        // In other words, it's val mapped linearly from range [min, max] to range [0, bits], and then rounded.
        int valInt = (int)MathF.Round((val - min) / (max - min) * bits);
        // val may be outside the range of min/max due to starting prototype values, so clamp.
        valInt = Math.Clamp(valInt, 0, bits);

        // Probability that the bit flip increases n.
        // The higher the current value is, the lower the probability of increasing value is, and the higher the probability of decreasive it it.
        // In other words, it tends to go to the middle.
        float probIncrease = 1 - (float)valInt / bits;
        int valIntMutated;
        if (_random.Prob(probIncrease))
        {
            valIntMutated = valInt + 1;
        }
        else
        {
            valIntMutated = valInt - 1;
        }

        // Set value based on mutated thermometer code.
        float valMutated = Math.Clamp((float)valIntMutated / bits * (max - min) + min, min, max);
        val = valMutated;
    }

    private void MutateInt(ref int val, int min, int max, int bits)
    {
        if (min == max)
        {
            val = min;
            return;
        }

        // Starting number of bits that are high, between 0 and bits.
        // In other words, it's val mapped linearly from range [min, max] to range [0, bits], and then rounded.
        int valInt = (int)MathF.Round((val - min) / (max - min) * bits);
        // val may be outside the range of min/max due to starting prototype values, so clamp.
        valInt = Math.Clamp(valInt, 0, bits);

        // Probability that the bit flip increases n.
        // The higher the current value is, the lower the probability of increasing value is, and the higher the probability of decreasing it.
        // In other words, it tends to go to the middle.
        float probIncrease = 1 - (float)valInt / bits;
        int valMutated;
        if (_random.Prob(probIncrease))
        {
            valMutated = val + 1;
        }
        else
        {
            valMutated = val - 1;
        }

        valMutated = Math.Clamp(valMutated, min, max);
        val = valMutated;
    }
}
