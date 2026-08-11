using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ActionSequence;

/// <summary>
/// Grants actions on MapInit and removes them on shutdown
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ActionSequenceSystem))]
public sealed partial class ActionSequenceComponent : Component
{
    [DataField(required: true)]
    public List<ActionSequence> Sequences = [];

    [DataField, AutoNetworkedField]
    public Dictionary<string, object> Blackboard = [];

    [DataField, AutoNetworkedField]
    public int CurrentStep;

    [DataField, AutoNetworkedField]
    public bool SequenceOngoing;
}
