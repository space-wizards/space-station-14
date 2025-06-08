using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Starlight.CloudEmotes;

[Prototype]
public sealed partial class CloudEmotePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public float AnimationTime = 3f;

    [DataField(required: true)]
    public SoundSpecifier? Sound;

    [DataField(required: true)]
    public SpriteSpecifier Icon { get; private set; } = default!;
}
