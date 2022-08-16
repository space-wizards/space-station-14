using Robust.Shared.Audio;

namespace Content.Server.Audio.Components;

[RegisterComponent]
public sealed class PlayOnSpawnComponent : Component
{
    [DataField("sound", required: true)]
    public SoundSpecifier? Sound;
}
