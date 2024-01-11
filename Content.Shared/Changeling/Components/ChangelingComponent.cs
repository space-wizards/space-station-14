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
    public float ArmBladeChemicalsCost = -20f;

    /// <summary>
    /// If the ling has an active armblade or not.
    /// </summary>
    [DataField]
    public bool ArmBladeActive = false;
    #endregion

    #region Chitinous Armor Ability
    /// <summary>
    /// The amount of chemicals that is needed to inflate the changeling's body into armor.
    /// </summary>
    [DataField]
    public float LingArmorChemicalsCost = -20f;

    /// <summary>
    /// The percent of how much chemical regeneration is reduced when the ling armor is active.
    /// </summary>
    [DataField]
    public float LingArmorRegenCost = 25f;

    /// <summary>
    /// If the ling has the armor on or not.
    /// </summary>
    [DataField]
    public bool LingArmorActive = false;
    #endregion

    [DataField]
    public EntityUid? ShopAction;
}
