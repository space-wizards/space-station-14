using Content.Shared.Destructible.Thresholds;
using Robust.Shared.GameStates;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

/// <summary>
/// This is used for a xenoarch trigger that self-activates at a regular interval
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(XATTimerSystem)), AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class XATTimerComponent : Component
{
    /// <summary>
    /// Next time timer going to activate.
    /// </summary>
    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan NextActivation;

    /// <summary>
    /// Delay between activations.
    /// </summary>
    [DataField, AutoNetworkedField]
    public MinMax PossibleDelayInSeconds;
}
