using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that a target stays alive.
/// A condition prototype must have another component in order to assign <see cref="Target"/>.
/// </summary>
[RegisterComponent, Access(typeof(KeepAliveConditionSystem))]
public sealed partial class KeepAliveConditionComponent : Component
{
    /// <summary>
    /// Mind id of the target that must be kept alive.
    /// This must be set by another system.
    /// </summary>
    [DataField("target"), ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? Target;
}
