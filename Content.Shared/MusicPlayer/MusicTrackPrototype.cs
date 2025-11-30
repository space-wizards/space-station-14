using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.MusicPlayer;

[Prototype]
public sealed partial class MusicTrackPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    [DataField(required: true)]
    public string Name = string.Empty;

    [DataField(required: true)]
    public SoundPathSpecifier Path = default!;

    [DataField(required: true)]
    public ProtoId<MusicCategoryPrototype> Category;
}
