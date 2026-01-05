using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;
using Robust.Shared.Random;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that changes a plant's stat by a random amount between a minimum and maximum value.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantChangeStatEntityEffectSystem : EntityEffectSystem<PlantComponent, PlantChangeStat>
{
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _entityEffects = default!;

    protected override void Effect(Entity<PlantComponent> entity, ref EntityEffectEvent<PlantChangeStat> args)
    {
        // if (_plantHolder.IsDead(entity.Owner))
        //     return;

        // var targetValue = args.Effect.TargetValue;
        // var targetComponent = args.Effect.TargetComponent;

        // if (!_componentFactory.TryGetRegistration(targetComponent, out var registration))
        // {
        //     Log.Error($"{nameof(PlantChangeStat)} Error: Component '{targetComponent}' is not a valid component name.");
        //     return;
        // }

        // if (!EntityManager.TryGetComponent(entity.Owner, registration.Type, out var plantComp))
        //     return;

        // var field = registration.Type.GetField(targetValue);
        // if (field == null)
        // {
        //     Log.Error(
        //         $"{nameof(PlantChangeStat)} Error: Field '{targetValue}' not found on component '{targetComponent}'. Did you misspell it?");
        //     return;
        // }

        // var currentValue = field.GetValue(plantComp);
        // if (currentValue == null)
        //     return;

        // float current;
        // switch (currentValue)
        // {
        //     case float f:
        //         current = f;
        //         break;
        //     case int i:
        //         current = i;
        //         break;
        //     default:
        //         Log.Error(
        //             $"{nameof(PlantChangeStat)} Error: Field '{targetValue}' on component '{targetComponent}' has unsupported type '{currentValue.GetType().Name}'.");
        //         return;
        // }

        // var min = args.Effect.MinValue;
        // var max = args.Effect.MaxValue;

        // // If the range is degenerate, only move toward that value.
        // if (MathHelper.CloseTo(min, max))
        // {
        //     if (MathHelper.CloseTo(current, min))
        //         return;

        //     _entityEffects.TryApplyEffect(entity.Owner,
        //         current < min ? args.Effect.Up : args.Effect.Down,
        //         args.Scale,
        //         args.User);
        //     return;
        // }

        // bool goUp;
        // if (current <= min)
        // {
        //     goUp = true;
        // }
        // else if (current >= max)
        // {
        //     goUp = false;
        // }
        // else
        // {
        //     var thermometer = (current - min) / (max - min);
        //     thermometer = Math.Clamp(thermometer, 0f, 1f);
        //     var probUp = 1f - thermometer;
        //     goUp = _random.Prob(probUp);
        // }

        // _entityEffects.TryApplyEffect(entity.Owner, goUp ? args.Effect.Up : args.Effect.Down, args.Scale, args.User);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantChangeStat : EntityEffectBase<PlantChangeStat>
{
    [DataField(required: true)]
    public string TargetValue;

    /// <summary>
    /// The plant component that contains <see cref="TargetValue"/>.
    /// </summary>
    [DataField(required: true)]
    public string TargetComponent;

    /// <summary>
    /// The minimum allowed value for the stat.
    /// </summary>
    [DataField(required: true)]
    public float MinValue;

    /// <summary>
    /// The maximum allowed value for the stat.
    /// </summary>
    [DataField(required: true)]
    public float MaxValue;

    /// <summary>
    /// Effect to apply when the stat should go up.
    /// </summary>
    [DataField(required: true)]
    public EntityEffect Up;

    /// <summary>
    /// Effect to apply when the stat should go down.
    /// </summary>
    [DataField(required: true)]
    public EntityEffect Down;
}
