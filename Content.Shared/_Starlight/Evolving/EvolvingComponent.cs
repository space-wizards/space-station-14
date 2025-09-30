using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Evolving;

[RegisterComponent]
[AutoGenerateComponentState]
public sealed partial class EvolvingComponent : Component
{
    /// <summary>
    /// The entity prototype to evolve into.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId EvolveTo; // TODO: Change to list and add ui. So you can select entity to evolve into.

    /// <summary>
    /// The id of action to trigger the evolution.
    /// </summary>
    [DataField]
    public string EvolveActionId = "ActionEvolve";

    /// <summary>
    /// The entity to use for the action to trigger the evolution.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? EvolveActionEntity;

    [DataField(serverOnly: true)]
    public List<EvolvingCondition> Conditions = [];
}