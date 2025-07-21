using Content.Shared.Actions.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Silicons.Borgs;

/// <summary>
/// This comp is added to station AI's invisible posibrain to handle shunting logics.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StationAIShuntableComponent : Component
{
    /// <summary>
    /// what action is granted to the ai to allow them to return to their body.
    /// </summary>
    [DataField]
    public EntProtoId<ActionComponent> UnshuntAction = "ActionAIUnShunt";

    /// <summary>
    /// What body is this AI currently inhabiting, null if the AI is in it's core.
    /// </summary>
    [ViewVariables]
    public EntityUid? Inhabited = null;
}
