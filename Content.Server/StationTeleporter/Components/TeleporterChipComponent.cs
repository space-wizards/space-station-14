using Content.Server.StationTeleporter.Systems;
using Robust.Shared.Audio;

namespace Content.Server.StationTeleporter.Components;

/// <summary>
/// Stores a reference to a specific teleporter. Can be inserted into the teleporter control console so that the console can control this teleporter
/// </summary>
[RegisterComponent]
[Access(typeof(StationTeleporterSystem))]
public sealed partial class TeleporterChipComponent : Component
{
    [DataField]
    public EntityUid? ConnectedTeleporter;

    [DataField]
    public string ConnectedName = string.Empty;

    /// <summary>
    /// When initialized, it will attempt to contact a random teleporter that has the same code. Can be used for pre-created teleporters
    /// </summary>
    /// <remarks>
    /// Not ProtoId<TagPrototype> because we can randomly generate this key for expeditions.
    /// </remarks>
    [DataField]
    public string? AutoLinkKey = null;

    /// <summary>
    /// If AutoLinkKey is not empty, but the link failed to be set up, this entity will be automatically deleted
    /// </summary>
    [DataField]
    public bool DeleteOnFailedLink = true;

    [DataField]
    public SoundSpecifier RecordSound = new SoundPathSpecifier("/Audio/Machines/high_tech_confirm.ogg")
    {
        Params = AudioParams.Default.WithVariation(0.05f),
    };
}
