using Robust.Shared.Audio;

namespace Content.Server.Audio.Components;

/// <summary>
///     Plays sound when assigned to component.
/// </summary>
[RegisterComponent]
public sealed class PlayOnSpawnComponent : Component
{
    [DataField("sound", required: true)]
    public SoundSpecifier? Sound;
}
