using Content.Shared.Mind;

namespace Content.Shared.Cloning;

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
    public MindComponent? Mind = default;

    /// <summary>
    /// EntityUid of the cloning pod (parent).
    /// </summary>
    [ViewVariables]
    public EntityUid Parent;
}
