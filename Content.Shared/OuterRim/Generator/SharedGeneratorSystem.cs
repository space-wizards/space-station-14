namespace Content.Shared.OuterRim.Generator;

/// <summary>
/// This handles small, portable generators that run off a material fuel.
/// </summary>
public abstract class SharedGeneratorSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {

    }

    public static float CalcFuelEfficiency(float targetPower, float optimalPower)
    {
        return MathF.Pow(optimalPower / targetPower, 1.3f);
    }
}
