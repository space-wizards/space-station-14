using Robust.Shared.Audio;

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

    #region Regenerate Ability
    /// <summary>
    /// The amount of chemicals that is needed to use the regenerate ability.
    /// </summary>
    public float RegenerateChemicalsCost = -10f;

    /// <summary>
    /// Sound that plays when the ling uses the regenerate ability.
    /// </summary>
    [DataField]
    public SoundSpecifier? SoundRegenerate = new SoundPathSpecifier("/Audio/Effects/demon_consume.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };
    #endregion

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
    /// The amount of chemical regeneration is reduced when the ling armor is active.
    /// </summary>
    [DataField]
    public float LingArmorRegenCost = 0.125f;

    /// <summary>
    /// If the ling has the armor on or not.
    /// </summary>
    [DataField]
    public bool LingArmorActive = false;
    #endregion

    #region Chameleon Skin Ability
    /// <summary>
    /// The amount of chemicals that is needed to activate chameleon skin ability.
    /// </summary>
    [DataField]
    public float ChameleonSkinChemicalsCost = -25f;

    /// <summary>
    /// If the ling has chameleon skin active or not.
    /// </summary>
    [DataField]
    public bool ChameleonSkinActive = false;

    /// <summary>
    /// How fast the changeling will turn invisible from standing still when using chameleon skin.
    /// </summary>
    [DataField]
    public float ChameleonSkinPassiveVisibilityRate = -0.10f;

    /// <summary>
    /// How fast the changeling will turn visible from movement when using chameleon skin.
    /// </summary>
    [DataField]
    public float ChameleonSkinMovementVisibilityRate = 0.10f;
    #endregion

    [DataField]
    public EntityUid? ShopAction;

    [DataField]
    public EntityUid? RegenAction;
}