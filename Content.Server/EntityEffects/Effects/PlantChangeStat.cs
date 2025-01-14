using Content.Server.Botany;
using Content.Server.Botany.Components;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.EntityEffects.Effects.PlantMetabolism;

[UsedImplicitly]
public sealed partial class PlantChangeStat : EntityEffect
{
    [DataField]
    public string TargetValue;

    [DataField]
    public float MinValue;

    [DataField]
    public float MaxValue;

    [DataField]
    public int Steps;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantHolder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);
        if (plantHolder == null || plantHolder.Seed == null)
            return;

        var member = plantHolder.Seed.GetType().GetField(TargetValue);
        var mutationSys = args.EntityManager.System<MutationSystem>();

        if (member == null)
        {
            mutationSys.Log.Error(this.GetType().Name + " Error: Member " + TargetValue + " not found on " + plantHolder.GetType().Name + ". Did you misspell it?");
            return;
        }

        var currentValObj = member.GetValue(plantHolder.Seed);
        if (currentValObj == null)
            return;

        if (member.FieldType == typeof(float))
        {
            var floatVal = (float)currentValObj;
            MutateFloat(ref floatVal, MinValue, MaxValue, Steps);
            member.SetValue(plantHolder.Seed, floatVal);
        }
        else if (member.FieldType == typeof(int))
        {
            var intVal = (int)currentValObj;
            MutateInt(ref intVal, (int)MinValue, (int)MaxValue, Steps);
            member.SetValue(plantHolder.Seed, intVal);
        }
        else if (member.FieldType == typeof(bool))
        {
            var boolVal = (bool)currentValObj;
            boolVal = !boolVal;
            member.SetValue(plantHolder.Seed, boolVal);
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
        if (Random(probIncrease))
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
        if (Random(probIncrease))
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

    private bool Random(float odds)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        return random.Prob(odds);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        throw new NotImplementedException();
    }
}
