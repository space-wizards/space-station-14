using Robust.Shared.Audio;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(NukeopsRuleSystem))]
public sealed partial class XenoborgsRuleComponent : Component
{
    /// <summary>
    ///     Path to the sound played when the xenoborg gets their role.
    /// </summary>
    [DataField]
    public SoundSpecifier GreetSoundNotification; // = new SoundPathSpecifier("/Audio/Ambience/Antag/???.ogg");
}
