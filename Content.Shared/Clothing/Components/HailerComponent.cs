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
    public EntityUid? User;

    /// <summary>
    /// Are the wires of the hailer currently cut ?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AreWiresCut = false;

    /// <summary>
    /// Can the interaction with tools happen ? (Screwing/Cutting)
    /// </summary>
    [DataField]
    public bool IsToolInteractible = true;

    /// <summary>
    /// Locale text for the description when examined
    /// </summary>
    [DataField]
    public string DescriptionLocale;

    /// <summary>
    /// Range value for the exclamation effect on humanoids
    /// </summary>
    [DataField]
    public float Distance = 0f;

    /// <summary>
    /// The name displayed as the speaker when hailing orders
    /// </summary>
    [DataField]
    public string ChatName;

    /// <summary>
    /// Delay when the hailer is used with a screwing tool to change aggression level
    /// </summary>
    [DataField]
    public float ScrewingDoAfterDelay = 3f;

    /// <summary>
    /// Delay when the hailer is used with a cutting tool
    /// </summary>
    [DataField]
    public float CuttingDoAfterDelay = 5f;

    /// <summary>
    /// How long until you can use the verb again to change aggression level
    /// </summary>
    [DataField]
    public TimeSpan VerbCooldown = TimeSpan.FromSeconds(1);

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
    /// In machines.yml
    /// </summary>
    [DataField]
    public SoundSpecifier SettingError = new SoundCollectionSpecifier("CargoError");

    /// <summary>
    /// The action that gets displayed when the gas mask is equipped.
    /// </summary>
    [DataField]
    public EntProtoId Action = "ActionSecHailer";

    /// <summary>
    /// Reference to the action for hailing.
    /// </summary>
    [DataField]
    public EntityUid? ActionEntity;

    /// <summary>
    /// Entity prototype to spawn when used, using the whistle one
    /// </summary>
    [DataField]
    public EntProtoId ExclamationEffect = "WhistleExclamation";

    /// <summary>
    /// The levels/states of hailing. Determines what soundcollection to use
    /// </summary>
    [DataField]
    public List<HailLevel>? HailLevels = [];

    /// <summary>
    /// Index for HailsLevels property
    /// </summary>
    [DataField, AutoNetworkedField]
    public int? HailLevelIndex;

    /// <summary>
    /// Locale text prefix for emag lines
    /// </summary>
    [DataField]
    public string? EmagLevelPrefix;

    /// <summary>
    /// Orders shown on the BUI radial menu and determines what soundcollection to use
    /// </summary>
    [DataField]
    public List<HailOrder> Orders = [];

    public HailLevel? CurrentHailLevel
    {
        get
        {
            if (HailLevels != null && HailLevelIndex != null)
            {
                return HailLevels[(int)HailLevelIndex];
            }
            return null;
        }
    }
}

[Serializable, NetSerializable]
public enum SecMaskVisuals : byte
{
    State
}

/// <summary>
/// Measure the level of the hails produced, ex: more or less aggressive
/// </summary>
[DataRecord, Serializable, NetSerializable]
public record struct HailLevel
{
    [DataField]
    public string Name;
}

[DataRecord, Serializable, NetSerializable]
public record struct HailOrder
{
    [DataField]
    public string? Name;

    /// <summary>
    /// What will be shown on the BUI radial menu ?
    /// </summary>
    [DataField]
    public string? Description;

    /// <summary>
    /// Icon shown on the BUI
    /// </summary>
    [DataField, AutoNetworkedField]
    public SpriteSpecifier? Icon;

    /// <summary>
    /// What sound collection to use
    /// Will add the hail level if relevant
    /// </summary>
    [DataField]
    public string? SoundCollection;

    /// <summary>
    /// What locale text to use
    /// Will add the hail level if relevant
    /// </summary>
    [DataField]
    public string? LocalePrefix;
}
