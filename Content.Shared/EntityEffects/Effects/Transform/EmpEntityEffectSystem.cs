using Content.Shared.Emp;

namespace Content.Shared.EntityEffects.Effects.Transform;

public sealed partial class EmpEntityEffectSystem : EntityEffectSystem<TransformComponent, Emp>
{
    [Dependency] private readonly SharedEmpSystem _emp = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<Emp> args)
    {
        var range = MathF.Min(args.Effect.RangeModifier * args.Scale, args.Effect.MaxRange);

        _emp.EmpPulse(_xform.GetMapCoordinates(entity, xform: entity.Comp), range, args.Effect.EnergyConsumption, args.Effect.Duration);
    }
}

public sealed class Emp : EntityEffectBase<Emp>
{
    /// <summary>
    ///     Impulse range per unit of quantity
    /// </summary>
    [DataField]
    public float RangeModifier = 0.5f;

    /// <summary>
    ///     Maximum impulse range
    /// </summary>
    [DataField]
    public float MaxRange = 10;

    /// <summary>
    ///     How much energy will be drain from sources
    /// </summary>
    [DataField]
    public float EnergyConsumption = 12500;

    /// <summary>
    ///     Amount of time entities will be disabled
    /// </summary>
    [DataField]
    public float Duration = 15;

}
