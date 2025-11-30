using Robust.Shared.Prototypes;

namespace Content.Shared.MusicPlayer;

[Prototype]
public sealed partial class MusicCategoryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    [DataField(required: true)]
    public string Name = string.Empty;

    [DataField]
    public int Order = 0;
}
