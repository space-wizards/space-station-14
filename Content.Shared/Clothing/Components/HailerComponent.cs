using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;


namespace Content.Shared.Clothing.Components;

/// <summary>
/// Handle the hails (audible orders to stop) coming from a security gas mask / swat mask
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class HailerComponent : Component
{
    /// <summary>
    /// The person wearing the mask
    /// </summary>
    public EntityUid? User = null;

    /// <summary>
    /// State of the sec mask to check if it can hail
    /// </summary>
    [DataField, AutoNetworkedField]
    public SecMaskState CurrentState = SecMaskState.Functional;

    /// <summary>
    /// Range value for the exclamation effect on humanoids
    /// </summary>
    [DataField]
    public float Distance = 0f;

    /// <summary>
    /// The name displayed as the speaker when hailing orders
    /// </summary>
    [DataField]
    public string? ChatName;

    /// <summary>
    /// Delay when the hailer is screwed to change aggression level
    /// </summary>
    [DataField]
    public float ScrewingDoAfterDelay = 3f;

    /// <summary>
    /// Delay when the hailer has its wires cut
    /// </summary>
    [DataField]
    public float CuttingDoAfterDelay = 5f;

    /// <summary>
    /// How long until you can use the verb again to change aggression level
    /// </summary>
    [DataField]
    public TimeSpan VerbCooldown = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Time where the verb will be ready to be used again
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan TimeVerbReady = TimeSpan.Zero;

    /// <summary>
    /// Is the gas mask an ERT one ?
    /// Then needs special voicelines and behavior
    /// </summary>
    [DataField]
    public bool IsERT = false;

    /// <summary>
    /// Is the gas mask an SWAT one ?
    /// We need to replace a voiceline in that case
    /// </summary>
    [DataField]
    public bool IsHOS = false;

    /// <summary>
    /// What localized line to replace in special circumstances
    /// </summary>
    [DataField]
    public Dictionary<string, string> ReplaceVoicelinesLocalizeForHOS = new();

    /// <summary>
    /// Index of the sound that need replacing for the HOS
    /// </summary>
    [DataField]
    public int SecHailHighIndexForHOS;

    /// <summary>
    /// Aggression level of the hailer, how aggressive is it ?
    /// </summary>
    [DataField, AutoNetworkedField]
    public AggresionState AggresionLevel = AggresionState.Low;

    /// <summary>
    /// Soundcollection of AggresionState.Low
    /// </summary>
    [DataField]
    public SoundSpecifier LowAggressionSounds = new SoundCollectionSpecifier("SecHailLow");

    /// <summary>
    /// Soundcollection of AggresionState.Medium
    /// </summary>
    [DataField]
    public SoundSpecifier MediumAggressionSounds = new SoundCollectionSpecifier("SecHailMedium");

    /// <summary>
    /// Soundcollection of AggresionState.High
    /// </summary>
    [DataField]
    public SoundSpecifier HighAggressionSounds = new SoundCollectionSpecifier("SecHailHigh");

    /// <summary>
    /// Soundcollection when Emagged
    /// </summary>
    [DataField]
    public SoundSpecifier EmagAggressionSounds = new SoundCollectionSpecifier("SecHailEmag");

    /// <summary>
    /// Soundcollection of when the mask is the ERT one
    /// </summary>
    [DataField]
    public SoundSpecifier ERTAggressionSounds = new SoundCollectionSpecifier("SecHailERT");

    /// <summary>
    /// Soundcollection for replacing one voiceline when it's the SWAT mask
    /// </summary>
    [DataField]
    public SoundSpecifier HOSReplaceSounds = new SoundCollectionSpecifier("SecHailHOS");

    /// <summary>
    /// Soundcollection of screwing sounds.
    /// </summary>
    [DataField]
    public SoundSpecifier ScrewedSounds = new SoundCollectionSpecifier("Screwdriver");

    /// <summary>
    /// Soundcollection of cutting sounds.
    /// </summary>
    [DataField]
    public SoundSpecifier CutSounds = new SoundCollectionSpecifier("Wirecutter");

    /// <summary>
    /// Soundcollection when interacting with the verb to increase aggression level
    /// </summary>
    [DataField]
    public SoundSpecifier SettingBeep = new SoundCollectionSpecifier("CargoToggleLimit");

    /// <summary>
    /// Soundcollection when interacting with the verb to increase aggression level and it fails
    /// </summary>
    [DataField]
    public SoundSpecifier SettingError = new SoundCollectionSpecifier("CargoError"); //Beep when hailer is used with verb  and it FAILS !! In machines.yml

    /// <summary>
    /// The action that gets displayed when the gas mask is equipped.
    /// </summary>
    [DataField]
    public EntProtoId Action = "ActionSecHailer";

    /// <summary>
    /// Reference to the action.
    /// </summary>
    [DataField]
    public EntityUid? ActionEntity;

    /// <summary>
    /// Entity prototype to spawn when used, using the whistle one
    /// </summary>
    [DataField]
    public EntProtoId ExclamationEffect = "WhistleExclamation";
}

[Serializable, NetSerializable]
public enum SecMaskVisuals : byte
{
    State
}

[Serializable, NetSerializable]
public enum SecMaskState : byte
{
    Functional,
    WiresCut
}

/// <summary>
/// How aggresive are the orders coming from the hailer ? Higher means more aggressive / shitsec
/// </summary>
[Serializable, NetSerializable]
public enum AggresionState : byte
{
    Low = 0,
    Medium = 1,
    High = 2
}
