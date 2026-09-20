using Content.Shared.Conditions.HelperConditions;

namespace Content.Shared.Conditions.Satisfier;

/// <summary>
/// A satisfier defines how a conditions return value is considered for True/False
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class Satisfier: IWithInverted
{
    public bool IsSatisfied(float value)
    {
        return Inverted != IsSatisfiedInternal(value);
    }

    protected abstract bool IsSatisfiedInternal(float value);

    /// <summary>
    /// If true, satisfier result will be inverted by evaluation system.
    /// </summary>
    [DataField]
    public bool Inverted { get; set; }
}
