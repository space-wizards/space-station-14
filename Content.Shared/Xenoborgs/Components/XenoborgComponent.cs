using Robust.Shared.Audio;

namespace Content.Shared.Xenoborgs.Components;

[RegisterComponent]
public sealed partial class XenoborgComponent : Component
{
    /// <summary>
    ///     Path to antagonist alert sound.
    /// </summary>
    [DataField]
    public SoundSpecifier GreetSoundNotification = new SoundPathSpecifier("/Audio/Ambience/Antag/xenoborg_start.ogg");
}
