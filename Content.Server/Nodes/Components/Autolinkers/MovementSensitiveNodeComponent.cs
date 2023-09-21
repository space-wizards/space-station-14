namespace Content.Server.Nodes.Components.Autolinkers;

/// <summary>
/// A component that makes graph nodes recalculate their edges when they are moved and/or rotated.
/// </summary>
[RegisterComponent]
public sealed partial class MovementSensitiveNodeComponent : Component
{
    /// <summary>
    /// Whether this component prompts node edge updates on pure rotations.
    /// This component _always_ prompts for edge updates on translational movement.
    /// </summary>
    [DataField("dirtyOnRotation")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool DirtyOnRotation = true;
}
