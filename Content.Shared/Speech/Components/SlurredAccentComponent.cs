using Content.Shared.Speech.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Speech.Components;

/// <summary>
/// Go houhhm... yo'r drrunkk...
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SlurredSystem))]
public sealed partial class SlurredAccentComponent : BaseAccentComponent
{
    /// <summary>
    /// Divisor applied to total seconds used to get the odds of slurred speech occuring.
    /// </summary>
    [DataField]
    public float SlurredModifier = 1100f;

    /// <summary>
    /// Minimum amount of time on the slurred accent for it to start taking effect.
    /// </summary>
    [DataField]
    public float SlurredThreshold = 80f;
}
