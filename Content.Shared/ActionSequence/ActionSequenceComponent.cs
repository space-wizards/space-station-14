using Robust.Shared.GameStates;

namespace Content.Shared.ActionSequence;

/// <summary>
/// Grants actions on MapInit and removes them on shutdown
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class ActionSequenceComponent : Component
{
    [DataField(required: true)]
    public List<ActionStep> Steps = [];

    [DataField]
    public Dictionary<string, object> Blackboard = [];

    [DataField, AutoNetworkedField]
    public int CurrentStep;

    [DataField, AutoNetworkedField]
    public bool SequenceOngoing;
}
