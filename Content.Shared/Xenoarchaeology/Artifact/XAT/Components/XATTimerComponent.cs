using Robust.Shared.GameStates;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

/// <summary>
/// This is used for a xenoarch trigger that self-activates at a regular interval
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(XATTimerSystem)), AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class XATTimerComponent : Component
{
    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan NextActivation;

    [DataField, AutoNetworkedField]
    public TimeSpan Delay;
}
