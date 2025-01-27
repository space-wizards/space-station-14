using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Announcements.Prototypes;

/// <summary>
///     Defines an announcer and their announcement file paths
/// </summary>
[Prototype("announcer")]
public sealed class AnnouncerPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    ///     A prefix to add to all announcement paths unless told not to by <see cref="AnnouncementData.IgnoreBasePath"/>
    /// </summary>
    /// <remarks>Paths always start in Resources/</remarks>
    [DataField("basePath")]
    public string BasePath { get; } = default!;

    /// <summary>
    ///     Audio parameters to apply to all announcement sounds unless overwritten by <see cref="AnnouncementData.AudioParams"/>
    /// </summary>
    [DataField("baseAudioParams")]
    public AudioParams? BaseAudioParams { get; }

    [DataField("announcements")]
    public AnnouncementData[] Announcements { get; } = default!;
}

/// <summary>
///     Defines a path to an announcement file and that announcement's ID
/// </summary>
[DataDefinition]
public sealed partial class AnnouncementData
{
    [DataField("id")]
    public string ID = default!;

    /// <summary>
    ///     If true, the <see cref="AnnouncerPrototype.BasePath"/> will not be prepended to this announcement's path
    /// </summary>
    [DataField("ignoreBasePath")]
    public bool IgnoreBasePath = false;

    /// <summary>
    ///     Where to look for the announcement audio file
    /// </summary>
    [DataField("path")]
    public string? Path;

    /// <summary>
    ///    Use a soundCollection instead of a single sound
    /// </summary>
    [DataField("collection"), ValidatePrototypeId<SoundCollectionPrototype>]
    public string? Collection;

    /// <summary>
    ///     Overrides the default announcement message for this announcement type
    /// </summary>
    [DataField("message")]
    public string? MessageOverride;

    /// <summary>
    ///     Audio parameters to apply to this announcement sound
    ///     Will override <see cref="AnnouncerPrototype.BaseAudioParams"/>
    /// </summary>
    [DataField("audioParams")]
    public AudioParams? AudioParams;
}
