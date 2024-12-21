using Robust.Shared.Audio;
using Robust.Shared.GameStates;
namespace Content.Shared.Lock.Lockpick;

/// <summary>
/// Allows the item to forcefully unlock entities with the LockComponent
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class LockpickComponent : Component
{
    /// <summary>
    /// Plays when starting the lockpicking
    /// </summary>
    [DataField]
    public SoundSpecifier StartSound = new SoundPathSpecifier("/Audio/Items/Lockpick/lockpick_start.ogg");

    /// <summary>
    /// Plays if the lockpicking succeds 
    /// </summary>
    [DataField]
    public SoundSpecifier EndSound = new SoundPathSpecifier("/Audio/Items/Lockpick/lockpick_end.ogg");

    /// <summary>
    /// DoAfter timer 
    /// </summary>
    [DataField]
    public TimeSpan LockpickTime = TimeSpan.FromSeconds(20);
}
