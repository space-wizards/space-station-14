using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that the station's AME is overloaded.
/// </summary>
[RegisterComponent, Access(typeof(OverloadAmeConditionSystem))]
public sealed partial class OverloadAmeConditionComponent : Component
{
    /// <summary>
    /// Has this objective been completed?
    /// </summary>
    public bool Completed = false;
}
