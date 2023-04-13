using Robust.Shared.Audio;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

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
    [ViewVariables(VVAccess.ReadWrite), DataField("chemicalCap")]
    public int ChemicalCap = 75;

    #endregion

    #region Abilities
        #region Evolution Menu

        #endregion
        #region Absorb DNA
        
        #endregion
        #region Arm Blade
        
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