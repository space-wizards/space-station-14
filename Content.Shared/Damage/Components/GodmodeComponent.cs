using Content.Shared.Damage.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Damage.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedGodmodeSystem))]
public sealed partial class GodmodeComponent : Component
{
    [DataField("wasMovedByPressure")]
    public bool WasMovedByPressure;

    [DataField("oldDamage")]
    public DamageSpecifier? OldDamage = null;
}
