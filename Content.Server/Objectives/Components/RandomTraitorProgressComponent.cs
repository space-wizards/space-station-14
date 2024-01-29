using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Sets the target for <see cref="HelpProgressConditionComponent"/> to a random traitor.
/// </summary>
[RegisterComponent, Access(typeof(HelpProgressConditionSystem))]
public sealed partial class RandomTraitorProgressComponent : Component
{
}
