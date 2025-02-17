using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Automatically assign objective as complete.
/// </summary>
[RegisterComponent, Access(typeof(AutoCompleteConditionSystem))]
public sealed partial class AutoCompleteConditionComponent : Component
{
}
