using Robust.Shared.Audio;

namespace Content.Shared.Madden;

/// <summary>
/// Used to mark entities that will "madden" players - turn them into "maddened" DAGD antagonists
/// </summary>
[RegisterComponent]
public sealed partial class MaddeningComponent : Component
{
    [DataField]
    public string? AnnouncementText;

    [DataField]
    public string? AnnouncementSender;

    [DataField]
    public SoundSpecifier Stinger = new SoundPathSpecifier("/Audio/Ambience/ambidanger2.ogg");

    public EntityUid? MaddenedEntity;
}
