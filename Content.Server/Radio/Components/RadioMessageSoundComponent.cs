using Robust.Shared.Audio;

namespace Content.Server.Radio.Components;

/// <summary>
/// Component that causes the entity that sends the radio message to add a sound to anyone who recevies the message.
/// </summary>
[RegisterComponent]
public sealed partial class RadioMessageSoundComponent : Component
{
    /// <summary>
    /// Sound that should be played when the message is receieved
    /// </summary>
    [ViewVariables, DataField]
    public SoundSpecifier Sound;
}
