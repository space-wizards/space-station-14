using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Administration.AdminAnnounce;

[Prototype]
public sealed partial class AdminAnnouncementPresetPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField(required: true)]
    public Color Color;

    [DataField(required: true)]
    public LocId Announcer;

    [DataField]
    public LocId? Message;

    [DataField]
    public LocId? Signature;

    [DataField]
    public SoundPathSpecifier? Sound;
}
