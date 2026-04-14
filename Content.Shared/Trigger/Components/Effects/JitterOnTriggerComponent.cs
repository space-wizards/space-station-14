using Content.Shared.Jittering;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Makes the entity play a jitter animation when triggered.
/// If TargetUser is true the user will jitter instead.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class JitterOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// The kind of jitter to play.
    /// </summary>
    [DataField, AutoNetworkedField]
    public JitterParameters Jitter;

    /// <summary>
    /// How long to play the jitter.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Time;

    /// <summary>
    /// Jitter duration is set if true, or accumulated if false.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Refresh;
}

