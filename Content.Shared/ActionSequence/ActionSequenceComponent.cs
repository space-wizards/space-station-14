using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.ActionSequence;

/// <summary>
/// Handles actions that call several different effects on after the other.
/// Uses a blackboard as a means of steps to share information between one another.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class ActionSequenceComponent : Component
{
    /// <summary>
    /// The <see cref="ActionStep"/> list this sequence will take.
    /// </summary>
    [DataField(required: true)]
    public List<ActionStep> Steps = [];

    /// <summary>
    /// The blackboard containing information given for the steps. Cleared when the action sequence ends.
    /// Information out of it should be retrieved only via <see cref="ActionStep.TryGetBlackboardData"/>.
    /// </summary>
    [DataField]
    public Dictionary<string, object> Blackboard = [];

    /// <summary>
    /// The current step this sequence is at.
    /// The first one would be "1", the second "2", and so on.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int CurrentStep;

    /// <summary>
    /// Whether the sequence is currently ongoing and able to step further.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool SequenceOngoing;

    /// <summary>
    /// Whether the sequence is awaiting for some kind of external input, such as a doAfter finishing.
    /// The sequence still counts as ongoing, even when awaiting for something else.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SequenceAwaiting Awaiting = SequenceAwaiting.None;

    /// <summary>
    /// Potentially added key when we finish awaiting.
    /// Should be set by the relevant <see cref="ActionStep"/> when we start awaiting.
    /// This will be the Target EntityUid when paired with <see cref="ActionSequenceEntityTargetEvent"/>
    /// Or EntityCoordinates when paired with <see cref="ActionSequenceEntityTargetEvent"/>
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? AwaitingKey;
}

[Serializable, NetSerializable]
public enum SequenceAwaiting : byte
{
    None, // The sequence is not waiting for anything.
    DoAfter, // The sequence is waiting for a DoAfter to finish.
    Reactivation // The sequence is waiting for the action to be re-used.
}
