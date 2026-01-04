using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;
using Robust.Shared.Random;

namespace Content.Server.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// This system mutates an inputted stat for a PlantHolder, only works for floats, integers, and bools.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantChangeStatEntityEffectSystem : EntityEffectSystem<PlantComponent, PlantChangeStat>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;

    protected override void Effect(Entity<PlantComponent> entity, ref EntityEffectEvent<PlantChangeStat> args)
    {
        if (_plantHolder.IsDead(entity.Owner))
            return;

        var targetValue = args.Effect.TargetValue;
        var targetComponent = args.Effect.TargetComponent;

        if (string.IsNullOrWhiteSpace(targetComponent))
            return;

        if (!_componentFactory.TryGetRegistration(targetComponent, out var registration))
        {
            Log.Error($"{nameof(PlantChangeStat)} Error: Component '{targetComponent}' is not a valid component name.");
            return;
        }

        if (!EntityManager.TryGetComponent(entity.Owner, registration.Type, out var plantComp))
            return;

        var field = registration.Type.GetField(targetValue);
        if (field == null)
        {
            Log.Error($"{nameof(PlantChangeStat)} Error: Field '{targetValue}' not found on component '{targetComponent}'. Did you misspell it?");
            return;
        }

        var currentValue = field.GetValue(plantComp);
        if (currentValue == null)
            return;

        if (TryGetValue<float>(currentValue, out var floatVal))
        {
            MutateFloat(ref floatVal, args.Effect.MinValue, args.Effect.MaxValue, args.Effect.Steps);
            field.SetValue(plantComp, floatVal);
            return;
        }

        if (TryGetValue<int>(currentValue, out var intVal))
        {
            MutateInt(ref intVal, (int)args.Effect.MinValue, (int)args.Effect.MaxValue, args.Effect.Steps);
            field.SetValue(plantComp, intVal);
            return;
        }

        if (TryGetValue<bool>(currentValue, out var boolVal))
        {
            field.SetValue(plantComp, !boolVal);
        }
    }

    private bool TryGetValue<T>(object value, out T? result)
    {
        result = default;
        if (value is T val)
        {
            result = val;
            return true;
        }

        return false;
    }

    // Thermometer-code helpers: map a value in [min, max] to an integer [0, bits] and move it by +-1 (biased toward the middle).
    private static int GetThermometerBitsHigh(float val, float min, float max, int bits)
    {
        var thermometer = (int) MathF.Round((val - min) / (max - min) * bits);
        return Math.Clamp(thermometer, 0, bits);
    }

    private int GetThermometerDelta(int thermometer, int bits)
    {
        var probIncrease = 1f - (float) thermometer / bits;
        return _random.Prob(probIncrease) ? 1 : -1;
    }

    // Mutate reference 'val' between 'min' and 'max' by pretending the value
    // is representable by a thermometer code with 'bits' number of bits and
    // randomly flipping some of them.
    private void MutateFloat(ref float val, float min, float max, int bits)
    {
        if (MathHelper.CloseTo(min, max))
        {
            val = min;
            return;
        }

        var thermometer = GetThermometerBitsHigh(val, min, max, bits);
        thermometer += GetThermometerDelta(thermometer, bits);
        val = Math.Clamp((float) thermometer / bits * (max - min) + min, min, max);
    }

    private void MutateInt(ref int val, int min, int max, int bits)
    {
        if (min == max)
        {
            val = min;
            return;
        }

        var thermometer = GetThermometerBitsHigh(val, min, max, bits);
        thermometer += GetThermometerDelta(thermometer, bits);
        val = Math.Clamp(val + thermometer, min, max);
    }
}
