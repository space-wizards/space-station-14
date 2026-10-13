using Content.Shared.Mind;

namespace Content.Shared.Cloning.Components;

/// <summary>
/// Added to a body that is being cloned. Tracks mind and parent pod entity.
/// </summary>
[RegisterComponent]
public sealed partial class BeingClonedComponent : Component
{
    /// <summary>
    /// The mind associated with the body being cloned, if any.
    /// </summary>
    [ViewVariables]
    public MindComponent? Mind = null;

    /// <summary>
    /// EntityUid of the cloning pod (parent).
    /// </summary>
    [DataField]
    public EntityUid Parent;
}
