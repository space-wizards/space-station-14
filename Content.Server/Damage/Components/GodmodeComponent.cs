using Content.Server.Damage.Systems;
using Content.Shared.Damage;

namespace Content.Server.Damage.Components;

/// <summary>
///
/// </summary>
[RegisterComponent, Access(typeof(GodmodeSystem))]
public sealed class GodmodeComponent : Component
{
    public bool WasMovedByPressure;
    public DamageSpecifier? OldDamage = null;
}
