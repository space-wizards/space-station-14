using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Sets the target for <see cref="KeepAliveConditionComponent"/> to a random traitor.
/// </summary>
[RegisterComponent, Access(typeof(KeepAliveConditionSystem))]
public sealed partial class RandomTraitorAliveComponent : Component
{
}
