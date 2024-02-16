using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Marker;

/// <summary>
/// Applies leech upon hitting a damage marker target.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class LeechOnMarkerComponent : Component
{
    // TODO: Can't network damagespecifiers yet last I checked.
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("leech", required: true)]
    public DamageSpecifier Leech = new();
}
