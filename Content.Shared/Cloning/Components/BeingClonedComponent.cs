using Content.Shared.Mind;
using Robust.Shared.GameStates;

namespace Content.Shared.Cloning.Components;

/// <summary>
/// Added to a body that is being cloned. Tracks mind and parent pod entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BeingClonedComponent : Component
{
    /// <summary>
    /// The mind associated with the body being cloned, if any.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public MindComponent? Mind = null;

    /// <summary>
    /// EntityUid of the cloning pod (parent).
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid Parent;
}
