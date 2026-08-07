using Content.Shared.Chemistry.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Clumsy.Components;

/// <summary>
/// Afflicted entity will occasionally use hyposprays on themselves instead of their target.
/// </summary>
/// <seealso cref="InjectorComponent"/>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ClumsyStatusEffectSystem))]
public sealed partial class ClumsyHypoStatusEffectComponent : Component
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
    public LocId? FailedMessage = "clumsy-hypospray-fail-message";
}
