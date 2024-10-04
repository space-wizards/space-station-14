using Robust.Shared.Audio;
namespace Content.Shared.Lock.Lockpick;

[RegisterComponent]
public sealed partial class LockpickComponent : Component
{
    //Plays when starting the lockpicking
    [DataField]
    public SoundSpecifier StartSound = new SoundPathSpecifier("/Audio/Items/Lockpick/lockpick_start.ogg");

    //Plays if the lockpicking succeds 
    [DataField]
    public SoundSpecifier EndSound = new SoundPathSpecifier("/Audio/Items/Lockpick/lockpick_end.ogg");

    //DoAfter timer 
    [DataField]
    public TimeSpan LockpickTime = TimeSpan.FromSeconds(20);
}
