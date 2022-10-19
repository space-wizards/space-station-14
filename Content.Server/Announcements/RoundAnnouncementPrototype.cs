using Content.Server.GameTicking.Presets;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.Announcements;

/// <summary>
/// Used for any announcements on the start of a round.
/// </summary>
[Prototype("roundAnnouncement")]
public readonly record struct RoundAnnouncementPrototype : IPrototype
{
    [IdDataFieldAttribute]
    public string ID { get; } = default!;

    [DataField("sound")] public readonly SoundSpecifier? Sound;

    [DataField("message")] public readonly string? Message;

    [DataField("presets", customTypeSerializer: typeof(PrototypeIdListSerializer<GamePresetPrototype>))]
    public readonly List<string> GamePresets = new();
}
