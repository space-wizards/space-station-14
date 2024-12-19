using Robust.Shared.Audio;

namespace Content.Shared.StationTeleporter.Components;

/// <summary>
/// Stores a reference to a specific teleporter. Can be inserted into the teleporter control console so that the console can control this teleporter
/// </summary>
[RegisterComponent, AutoGenerateComponentState]
[Access(typeof(SharedStationTeleporterSystem))]
public sealed partial class TeleporterChipComponent : Component
{
    /// <summary>
    /// Uid of the teleporter this chip is synced with.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ConnectedTeleporter;

    /// <summary>
    /// The name of the teleporter the chip has synced is copied into this field. This information is used when examining the chip.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string ConnectedName = string.Empty;

    /// <summary>
    /// When initialized, it will attempt to contact a random teleporter that has the same code. Can be used for pre-created teleporters
    /// </summary>
    /// <remarks>
    /// Not ProtoId<TagPrototype> because we can randomly generate this key for expeditions.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public string? AutoLinkKey;

    [DataField]
    public SoundSpecifier RecordSound = new SoundPathSpecifier("/Audio/Machines/high_tech_confirm.ogg")
    {
        Params = AudioParams.Default.WithVariation(0.05f),
    };
}
