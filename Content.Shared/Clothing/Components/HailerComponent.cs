using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;


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

    [DataField]
    public List<HailLevel> HailLevels = new();

    /// <summary>
    /// Index for HailsLevels
    /// </summary>
    [DataField, AutoNetworkedField]
    public int HailLevelIndex;

    [DataField]
    public List<HailOrder> Orders = new();
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
/// Measure the level of the hails produced, ex: more or less aggressive
/// </summary>
[DataRecord, Serializable, NetSerializable]
public record struct HailLevel
{
    public HailLevel() => Name = String.Empty;

    [DataField]
    public string Name;

    [DataField]
    public bool Cyclable = true;
}

[DataRecord, Serializable, NetSerializable]
public record struct HailOrder
{
    [DataField]
    public string? Name;

    [DataField]
    public string? Description;

    [DataField, AutoNetworkedField]
    public SpriteSpecifier? Icon; //= new SpriteSpecifier.Texture(new("Interface/Actions/scream.png"));

    [DataField]
    public string? SoundCollection;

    [DataField]
    public string? LocalePrefix;
}
