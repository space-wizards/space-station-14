namespace Content.Server.Changeling;

// this could use LimitedCharges and AutoRecharge however:
// 1. examining someone to see they have 75 charges remaining must be disabled
// 2. need to make it easier to need and use multiple charges
public partial class ChangelingComponent : Component
{
    /// <summary>
    /// Number of chemicals the changeling has, for some stings and abilities.
    /// Passively regenerates at a rate modified by certain abilities.
    /// </summary>
    [DataField("chemicals"), ViewVariables(VVAccess.ReadWrite)]
    public int Chemicals = 75;

    /// <summary>
    /// Maximum number of chemicals you can regenerate up to.
    /// Absorbing a changeling ignores this limit.
    /// </summary>
    [DataField("maxChemicals"), ViewVariables(VVAccess.ReadWrite)]
    public int MaxChemicals = 75;

    /// <summary>
    /// Seconds it takes to regenerate a chemical.
    /// </summary>
    [DataField("chemicalRegenTime"), ViewVariables(VVAccess.ReadWrite)]
    public float ChemicalRegenTime = 1.0f;
}
