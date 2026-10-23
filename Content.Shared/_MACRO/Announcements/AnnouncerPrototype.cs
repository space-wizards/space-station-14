using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._MACRO.Announcements;

/// <summary>
/// This is a prototype for announcer variants
/// </summary>
[Prototype]
public sealed partial class AnnouncerPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public required Dictionary<ProtoId<AnnouncementSoundPrototype>, SoundSpecifier> Sounds;
}
