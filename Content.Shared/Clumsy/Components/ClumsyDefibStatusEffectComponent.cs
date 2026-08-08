using Content.Shared.Medical;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Clumsy.Components;

/// <summary>
/// Afflicted entity will occasionally shock itself while using a defibrillator.
/// </summary>
/// <seealso cref="DefibrillatorComponent"/>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ClumsyStatusEffectSystem))]
public sealed partial class ClumsyDefibStatusEffectComponent : Component
{
    /// <summary>
    /// How often to fail.
    /// </summary>
    [DataField]
    public float ClumsyChance = 0.5f;

    /// <summary>
    /// Sound played upon failure.
    /// </summary>
    [DataField]
    public SoundSpecifier? ClumsySound = new SoundPathSpecifier("/Audio/Items/bikehorn.ogg");

    /// <summary>
    /// Popup played to the afflicted when they fail.
    /// </summary>
    [DataField]
    public LocId? SelfFailedMessage; //todo

    /// <summary>
    /// Popup played to others when the afflicted fails.
    /// </summary>
    [DataField]
    public LocId? OtherFailedMessage; //todo
}
