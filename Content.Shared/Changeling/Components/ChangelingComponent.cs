using Robust.Shared.Audio;
using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;

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

    /// <summary>
    /// The maximum amount of DNA strands a ling can have at one time
    /// </summary>
    [DataField]
    public int DNAStrandCap = 10;

    /// <summary>
    /// List of stolen DNA
    /// </summary>
    [DataField]
    public List<PolymorphHumanoidData> StoredDNA = new List<PolymorphHumanoidData>();

    /// <summary>
    /// The DNA index that the changeling currently has selected
    /// </summary>
    [DataField]
    public int SelectedDNA = 0;

    #region Chemical Costs
    [DataField]
    public float ChemicalsCostFive = -5f;

    [DataField]
    public float ChemicalsCostTen = -10f;

    [DataField]
    public float ChemicalsCostFifteen = -15f;

    [DataField]
    public float ChemicalsCostTwenty = -20f;

    [DataField]
    public float ChemicalsCostTwentyFive = -25f;

    [DataField]
    public float ChemicalsCostFifty = -50f;
    #endregion

    #region DNA Absorb Ability
    /// <summary>
    /// How long an absorb stage takes, in seconds.
    /// </summary>
    [DataField]
    public int AbsorbDuration = 15;

    /// <summary>
    /// The stage of absorbing that the changeling is on. Maximum of 2 stages.
    /// </summary>
    [DataField]
    public int AbsorbStage = 0;

    /// <summary>
    /// The amount of genetic damage the target gains when they're absorbed.
    /// </summary>
    [DataField]
    public float AbsorbGeneticDmg = 200.0f;

    /// <summary>
    /// The amount of evolution points the changeling gains when they absorb another changeling.
    /// </summary>
    [DataField]
    public float AbsorbedChangelingPointsAmount = 5.0f;
    #endregion

    #region Regenerate Ability
    /// <summary>
    /// The amount of burn damage is healed when the regenerate ability is sucesssfully used.
    /// </summary>
    [DataField]
    public float RegenerateBurnHealAmount = -100f;

    /// <summary>
    /// The amount of brute damage is healed when the regenerate ability is sucesssfully used.
    /// </summary>
    [DataField]
    public float RegenerateBruteHealAmount = -125f;

    /// <summary>
    /// The amount of blood volume that is gained when the regenerate ability is sucesssfully used.
    /// </summary>
    [DataField]
    public float RegenerateBloodVolumeHealAmount = 1000f;

    /// <summary>
    /// The amount of bleeding that is reduced when the regenerate ability is sucesssfully used.
    /// </summary>
    [DataField]
    public float RegenerateBleedReduceAmount = -1000f;

    /// <summary>
    /// Sound that plays when the ling uses the regenerate ability.
    /// </summary>
    [DataField]
    public SoundSpecifier? SoundRegenerate = new SoundPathSpecifier("/Audio/Effects/demon_consume.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };
    #endregion

    [DataField]
    public SoundSpecifier? SoundFlesh = new SoundPathSpecifier("/Audio/Effects/blobattack.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    #region Armblade Ability
    /// <summary>
    /// If the ling has an active armblade or not.
    /// </summary>
    [DataField]
    public bool ArmBladeActive = false;
    #endregion

    #region Chitinous Armor Ability
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

    #region Dissonant Shriek Ability
    /// <summary>
    /// Range of the Dissonant Shriek's EMP in tiles.
    /// </summary>
    [DataField]
    public float DissonantShriekEmpRange = 2.75f;

    /// <summary>
    /// Power consumed from batteries by the Dissonant Shriek's EMP
    /// </summary>
    [DataField]
    public float DissonantShriekEmpConsumption = 50000f;

    /// <summary>
    /// How long the Dissonant Shriek's EMP effects last for
    /// </summary>
    [DataField]
    public float DissonantShriekEmpDuration = 12f;
    #endregion

    [DataField]
    public float Accumulator = 0f;
}