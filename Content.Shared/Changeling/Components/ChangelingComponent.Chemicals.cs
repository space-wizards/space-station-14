namespace Content.Shared.Changeling.Components;

public abstract partial class ChangelingComponent : Component
{
    /// <summary>
    /// Number of chemicals the changeling has, for some stings and abilities.
    /// Passively regenerates at a rate modified by certain abilities.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Chemicals = 75;

    /// <summary>
    /// Maximum number of chemicals you can regenerate up to.
    /// Absorbing people can increase this limit.
    /// </summary>
    [DataField]
    public int MaxChemicals = 75;

    /// <summary>
    /// Seconds it takes to regenerate a chemical.
    /// </summary>
    [DataField]
    public float ChemicalRegenTime = 1.0f;
}