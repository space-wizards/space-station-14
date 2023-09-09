using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that a target dies or, if <see cref="RequireDead"/> is false, is not on the emergency shuttle.
/// A condition prototype must have another component in order to assign <see cref="Target"/>.
/// </summary>
[RegisterComponent, Access(typeof(KillPersonConditionSystem))]
public sealed partial class KillPersonConditionComponent : Component
{
    /// <summary>
    /// Mind entity id of the target to kill.
    /// This must be set by another system.
    /// </summary>
    [DataField("target"), ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? Target;

    /// <summary>
    /// Whether the target must be truly dead, ignores missing evac.
    /// </summary>
    [DataField("requireDead"), ViewVariables(VVAccess.ReadWrite)]
    public bool RequireDead = false;

    /// <summary>
    /// Locale id for the objective title.
    /// It is passed "targetName" and "job" arguments.
    /// </summary>
    [DataField("title"), ViewVariables(VVAccess.ReadWrite)]
    public string Title = "objective-condition-kill-person-title";
}
