using Robust.Shared.Audio;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.GameStates;
using Content.Shared.Humanoid;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Shared.Changeling;

[RegisterComponent]
[NetworkedComponent]
public sealed class ChangelingComponent : Component
{
    #region Points and Chemicals

    [ViewVariables(VVAccess.ReadOnly)]
    public string StoreCurrencyName = "Points";

    [ViewVariables(VVAccess.ReadOnly)]
    public string AbilityCurrencyName = "Chemicals";

    /// <summary>
    /// Stating points that the changeling will have, they can be spent on abilities
    /// </summary>
    [DataField("startingPoints")]
     public int StartingPoints = 10;

    /// <summary>
    /// Starting chemicals that the changeling will have at the start, they can be spent on using abilities
    /// </summary>
    [DataField("startingChemicals")]
    public int StartingChemicals = 10;

    /// <summary>
    /// Chemical regeneration rate per X seconds
    /// </summary>
    [DataField("chemicalRegenRate")]
    public int ChemicalRegenRate = 1;

    /// <summary>
    /// Chemical regeneration regeneration time in seconds
    /// </summary>
    [DataField("chemicalRegenTime")]
    public float ChemicalRegenTime = 2f;

    /// <summary>
    /// Chemical amount limit
    /// </summary>
    [DataField("chemicalRegenCap")]
    public float ChemicalRegenCap = 75;

    /// <summary>
    /// DNA strands amount limit
    /// </summary>
    [DataField("DNAStrandCap")]
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


    #endregion


    /// <summary>
    /// List of absorbed entities
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("entitiesAbsorbed")]
    public List<HumanoidData> StoredHumanoids = new List<HumanoidData>();



    #region Abilities

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


    /// <summary>
    /// DNA Sting cost in chemicals
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("DNAStingCost")]
    public int DNAStingCost = 25;


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

    /// <summary>
    /// "Is the armblade activated or not"
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool ArmBladeActivated = false;

    [ViewVariables(VVAccess.ReadWrite)]
    public int ArmBladeMaxHands = 1;

    #endregion

}

/// <summary>
/// Struct used to store the data of players, used to spawn a copy of a player
/// </summary>
public struct HumanoidData
{
    public EntityPrototype? EntityPrototype;

    public MetaDataComponent? MetaDataComponent;

    public HumanoidAppearanceComponent? AppearanceComponent;

    public string? Dna;

    public EntityUid? EntityUid;
}
