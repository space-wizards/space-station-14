using Content.Server.Corvax.Objectives.Systems;

namespace Content.Server.Corvax.Objectives.Components;

/// <summary>
/// Requires that the player dies to be complete.
/// </summary>
[RegisterComponent, Access(typeof(HijackShuttleSystem))]
public sealed partial class HijackShuttleConditionComponent : Component
{
}
