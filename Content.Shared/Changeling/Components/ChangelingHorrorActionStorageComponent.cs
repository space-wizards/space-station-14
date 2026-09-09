using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Changeling.Components;

/// <summary>
/// Component used to store changeling horror form-exclusive actions.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ChangelingHorrorActionStorageComponent : Component
{
    /// <summary>
    /// The actions that will be granted in horror mode
    /// </summary>
    [DataField, AutoNetworkedField, AlwaysPushInheritance]
    public List<EntProtoId> Actions = [];

    /// <summary>
    /// The actions that were granted and that will be deleted when turning back.
    /// </summary>
    [AutoNetworkedField]
    public List<EntityUid> CreatedActions = [];
}
