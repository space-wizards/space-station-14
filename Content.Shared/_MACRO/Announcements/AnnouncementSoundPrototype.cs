using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._MACRO.Announcements;

/// <summary>
/// This is a prototype for an announcement sound
/// </summary>
[Prototype]
public sealed partial class AnnouncementSoundPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public required SoundSpecifier DefaultSound;
}
