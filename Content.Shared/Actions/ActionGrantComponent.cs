using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Actions;

/// <summary>
/// Grants actions on MapInit and removes them on shutdown
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(ActionGrantSystem))]
public sealed partial class ActionGrantComponent : Component
{
    /// <summary>
    /// The list of actions to add to this entity on <see cref="MapInitEvent"/>.
    /// </summary>
    [DataField(required: true), AutoNetworkedField, AlwaysPushInheritance]
    public List<EntProtoId> Actions = new();

    /// <summary>
    /// The EntityUid of the actions added by this component.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntityUid> ActionEntities = new();

    /// <summary>
    /// Whether the actions should be removed on <see cref="ComponentShutdown"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RemoveOnShutdown = true;
}
