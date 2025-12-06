using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Melee.Events;

/// <summary>
/// Raised to get the effective melee attacker entity (for range/origin/animation),
/// which may differ from the controlling user.
/// </summary>
[ByRefEvent]
public struct GetMeleeAttackerEntityEvent
{
	public EntityUid? Attacker;
	public bool Handled;
}
