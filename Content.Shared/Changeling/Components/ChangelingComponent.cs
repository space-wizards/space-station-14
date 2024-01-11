namespace Content.Shared.Changeling.Components;

[RegisterComponent]
public sealed partial class ChangelingComponent : Component
{
    /// <summary>
    /// The amount of chemicals the ling has.
    /// </summary>
    [DataField]
    public float Chemicals = 20f;

    /// <summary>
    /// The amount of chemicals passively generated per second
    /// </summary>
    [DataField]
    public float ChemicalsPerSecond = 0.5f;

    /// <summary>
    /// The lings's current max amount of chemicals.
    /// </summary>
    [DataField]
    public float MaxChemicals = 75f;

    [DataField]
    public float Accumulator = 0f;

    #region Armblade Ability
    /// <summary>
    /// The amount of chemicals that is needed to use the arm blade.
    /// </summary>
    [DataField]
    public float ArmBladeChemicalsCost = 20f;

    /// <summary>
    /// If the ling has an active armblade or not.
    /// </summary>
    [DataField]
    public bool ArmBladeActive = false;
    #endregion

    [DataField]
    public EntityUid? Action;
}
