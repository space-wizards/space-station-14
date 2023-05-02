using Robust.Shared.Audio;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.Humanoid;
using Robust.Shared.GameObjects;

namespace Content.Shared.Changeling;

[RegisterComponent]
[NetworkedComponent]
public sealed class ChangelingComponent : Component
{
    #region Points and Chemicals
    /// <summary>
    /// Stating points that the changeling will have, they can be spent on abilities
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("startingPoints")]
     public int StartingPoints = 10;

    /// <summary>
    /// Starting chemicals that the changeling will have at the start, they can be spent on using abilities
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("startingChemicals")]
    public int StartingChemicals = 10;

    /// <summary>
    /// Chemical regeneration rate per X seconds
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("chemicalRegenRate")]
    public int ChemicalRegenRate = 1;

    /// <summary>
    /// Chemical regeneration regeneration time in seconds
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("chemicalRegenTime")]
    public float ChemicalRegenTime = 2f;

    /// <summary>
    /// Chemical amount limit
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("chemicalRegenCap")]
    public float ChemicalRegenCap = 75;

    /// <summary>
    /// DNA strands amount limit
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("DNAStrandCap")]
    public int DNAStrandCap = 7;

    /// <summary>
    /// DNA Strands balance
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("DNAStrandBalance")]
    public int DNAStrandBalance = 0;

    /// <summary>
    /// Chemicals balance
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("chemicalBalance")]
    public int ChemicalBalance = 0;

    /// <summary>
    /// Points balace
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("pointBalance")]
    public int PointBalance = 0;

    /// <summary>
    /// List of absorbed entities
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("entitiesAbsorbed")]
    public List<HumanoidData> storedHumanoids = new List<HumanoidData>();


    #endregion

    #region Abilities
        #region Evolution Menu

        #endregion
        #region Absorb DNA
    /// <summary>
    /// DNA absorption cost in chemicals
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("AbsorbDNACost")]
    public int AbsorbDNACost = 0;

    /// <summary>
    /// DNA absorption delay time
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("AbsorbDNADelay")]
    public float AbsorbDNADelay = 10f;

        #endregion
        #region DNA Sting

    /// <summary>
    /// DNA Sting cost in chemicals
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("DNAStingCost")]
    public int DNAStingCost = 25;
        
        #endregion
        #region Arm Blade
    /// <summary>
    /// Arm blade cost in chemicals
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("armBladeCost")]
    public int ArmBladeCost = 25;

    /// <summary>
    /// Arm blade cost in points
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("armBladeBuyCost")]
    public int ArmBladeBuyCost = 2;
        #endregion
    #endregion


    // /// <summary>
    // /// timings for giggles and knocks.
    // /// </summary>
    // [ViewVariables(VVAccess.ReadWrite)]
    // public TimeSpan DamageGiggleCooldown = TimeSpan.FromSeconds(2);

    // [ViewVariables(VVAccess.ReadWrite)]
    // public float KnockChance = 0.05f;

    // [ViewVariables(VVAccess.ReadWrite)]
    // public float GiggleRandomChance = 0.1f;

    // [DataField("emoteId", customTypeSerializer: typeof(PrototypeIdSerializer<EmoteSoundsPrototype>))]
    // public string? EmoteSoundsId = "Cluwne";

    // /// <summary>
    // /// Amount of time cluwne is paralyzed for when falling over.
    // /// </summary>
    // [ViewVariables(VVAccess.ReadWrite)]
    // public float ParalyzeTime = 2f;

    // /// <summary>
    // /// Sound specifiers for honk and knock.
    // /// </summary>
    // [DataField("spawnsound")]
    // public SoundSpecifier SpawnSound = new SoundPathSpecifier("/Audio/Items/bikehorn.ogg");

    // [DataField("knocksound")]
    // public SoundSpecifier KnockSound = new SoundPathSpecifier("/Audio/Items/airhorn.ogg");
}

public struct HumanoidData
{
    public MetaDataComponent _metaDataComponent;

    public HumanoidAppearanceComponent _appearanceComponent;
}