using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that a target completes half of their objectives.
/// A condition prototype must have another component in order to assign <see cref="Target"/>.
/// </summary>
[RegisterComponent, Access(typeof(HelpProgressConditionSystem))]
public sealed partial class HelpProgressConditionComponent : Component
{
    /// <summary>
    /// Mind id of the target that has to be helped.
    /// This must be set by another system.
    /// </summary>
    [DataField("target"), ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? Target;
}
