using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Clumsy;

/// <summary>
/// A simple clumsy tag-component.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ClumsyComponent : Component
{
    /// <summary>
    ///     Sound to play when clumsy interactions fail.
    /// </summary>
    [DataField]
    public SoundSpecifier ClumsySound = new SoundPathSpecifier("/Audio/Items/bikehorn.ogg");

    /// <summary>
    ///     Default chance to fail a clumsy interaction.
    ///     If a system needs to use something else, add a new variable here do not modify this percentage.
    /// </summary>
    [DataField]
    public float ClumsyDefaultCheck = 0.5f;

    /// <summary>
    ///     Default stun time.
    ///     If a system needs to use something else, add a new variable here do not modify this number.
    /// </summary>
    [DataField]
    public TimeSpan ClumsyDefaultStunTime = TimeSpan.FromSeconds(2.5);
}
