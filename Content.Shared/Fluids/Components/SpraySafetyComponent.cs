using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Fluids.Components;

/// <summary>
/// Uses <c>ItemToggle</c> to control safety for a spray item.
/// You can't spray or refill it while safety is on.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SpraySafetySystem))]
public sealed partial class SpraySafetyComponent : Component
{
    /// <summary>
    /// Popup shown when trying to spray or refill with safety on.
    /// </summary>
    [DataField]
    public LocId Popup = "fire-extinguisher-component-safety-on-message";

    /// <summary>
    /// Sound to play after refilling.
    /// </summary>
    [DataField]
    public SoundSpecifier RefillSound = new SoundPathSpecifier("/Audio/Effects/refill.ogg");
}
