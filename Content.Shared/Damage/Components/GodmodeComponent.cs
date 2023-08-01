using Content.Shared.Damage.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Damage.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedGodmodeSystem))]
public sealed class GodmodeComponent : Component
{
    public bool WasMovedByPressure;
    public DamageSpecifier? OldDamage = null;
}
