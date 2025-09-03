using Robust.Shared.Audio;

namespace Content.Shared.Xenoborgs.Components;

/// <summary>
/// Defines what is a xenoborg core for the intentions of the xenoborg rule. if all xenoborg cores are destroyed. all xenoborgs will self-destruct.
/// </summary>
[RegisterComponent]
public sealed partial class MothershipCoreComponent : Component
{
    /// <summary>
    ///     Path to antagonist alert sound.
    /// </summary>
    [DataField]
    public SoundSpecifier GreetSoundNotification = new SoundPathSpecifier("/Audio/Ambience/Antag/xenoborg_start.ogg");
}
