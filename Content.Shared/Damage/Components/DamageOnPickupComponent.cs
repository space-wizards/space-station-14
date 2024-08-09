using Content.Shared.Damage.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Damage.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class DamageOnPickupComponent : Component
{
    [DataField]
    public string Sound = string.Empty;

    [DataField("damage")]
    public DamageSpecifier Damage = new();
}
