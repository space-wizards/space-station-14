using Content.Server.Changeling;
using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

[RegisterComponent, Access(typeof(ChangelingObjectiveSystem), typeof(ChangelingSystem))]
public sealed partial class AbsorbTargetConditionComponent : Component
{
    /// <summary>
    /// Whether the target must be truly dead, ignores missing evac.
    /// Kill objectives have this so I might as well leave this as an option here too.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool RequireDead = true;

    /// <summary>
    /// Becomes true once the target has been absorbed
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Absorbed = false;
}
