using Robust.Shared.Prototypes;

namespace Content.Shared.Jukebox;

[Prototype("musicList")]
public sealed class MusicListPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = string.Empty;
    [DataField("songs")] public MusicListDefinition[] Songs { get; private set; } = Array.Empty<MusicListDefinition>();
}

[DataDefinition]
public sealed partial class MusicListDefinition
{
    [DataField("name", required: true)] public string Name { get; private set; } = string.Empty;
    [DataField("path", required: true)] public string Path { get; private set; } = string.Empty;
    [DataField("copyright")] public string Copyright { get; private set; } = string.Empty;
    [DataField("songLength")] public float SongLength { get; private set; } = 0.0f;
}
