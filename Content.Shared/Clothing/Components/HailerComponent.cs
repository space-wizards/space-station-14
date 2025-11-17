using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;


namespace Content.Shared.Clothing.Components;

/// <summary>
/// Handle the orders coming from a security gas mask / swat mask
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class HailerComponent : Component
{
    /// <summary>
    /// Are the wires of the hailer cut ?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AreWiresCut;

    /// <summary>
    /// The name displayed as the speaker when hailing orders
    /// </summary>
    [DataField]
    public string ChatName;

    /// <summary>
    /// Delay when the hailer is used with a cutting tool
    /// </summary>
    [DataField]
    public float CuttingDoAfterDelay;

    /// <summary>
    /// Delay when the hailer is used with a screwing tool to change aggression level
    /// </summary>
    [DataField]
    public float ScrewingDoAfterDelay;

    /// <summary>
    /// Loc string for the description when examined
    /// </summary>
    [DataField]
    public string DescriptionLocale;

    /// <summary>
    /// Range value for the exclamation effect on humanoids
    /// </summary>
    [DataField]
    public float Distance;

    /// <summary>
    /// Loc string prefix for emag lines
    /// </summary>
    [DataField]
    public string? EmagLevelPrefix;

    /// <summary>
    /// Entity prototype spawn on other people when using the hailer. Similar to the whistle.
    /// </summary>
    [DataField]
    public EntProtoId ExclamationEffect;

    /// <summary>
    /// This determines what soundcollection and what loc string will be used in addition to a random index
    /// </summary>
    [DataField]
    public List<HailLevel>? HailLevels = [];

    /// <summary>
    /// Index for HailsLevels property
    /// </summary>
    [DataField, AutoNetworkedField]
    public int? HailLevelIndex;

    /// <summary>
    /// Can the interaction with tools happen ? (Screwing/Cutting)
    /// </summary>
    [DataField]
    public bool IsToolInteractible;

    /// <summary>
    /// Orders shown on the BUI radial menu and determines what soundcollection to use when playing the audio of the line
    /// </summary>
    [DataField]
    public List<HailOrder> Orders = [];

    /// <summary>
    /// How long until you can use the verb again to change aggression level
    /// </summary>
    [DataField]
    public TimeSpan VerbCooldown = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Time where the verb will be ready to be used again
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField, AutoNetworkedField]
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
    /// Return the current HailLevel based on HailLevelIndex or return null
    /// </summary>
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

    /// <summary>
    /// The user wearing the mask
    /// </summary>
    public EntityUid? User;
}

[Serializable, NetSerializable]
public enum SecMaskVisuals : byte
{
    State
}

/// <summary>
/// Category of hailer line. Used for determining which soundcollection and loc string to use
/// </summary>
[DataRecord, Serializable, NetSerializable]
public record struct HailLevel
{
    [DataField]
    public string Name;
}

/// <summary>
/// Appears on the BUI radial menu of the hailer
/// Each has a soundCollection and loc string prefix used when choosen on the BUI
/// </summary>
[DataRecord, Serializable, NetSerializable]
public record struct HailOrder
{
    [DataField]
    public string? Name;

    /// <summary>
    /// String shown the BUI radial menu
    /// </summary>
    [DataField]
    public string? Description;

    /// <summary>
    /// Icon shown on the BUI radial menu
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
    /// What loc string to use
    /// Will add the hail level if relevant
    /// </summary>
    [DataField]
    public string? LocalePrefix;
}
