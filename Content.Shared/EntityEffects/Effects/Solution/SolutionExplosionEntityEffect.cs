namespace Content.Shared.EntityEffects.Effects.Solution;

/// <summary>
/// See serverside system.
/// </summary>
/// <inheritdoc cref="EntityEffect"/>
public sealed partial class SolutionExplosion : EntityEffectBase<SolutionExplosion>
{
    /// <summary>
    /// The name of the solution to spill and scale the explosion by.
    /// </summary>
    [DataField(required: true)]
    public string Solution = string.Empty;
}
