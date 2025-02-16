using Content.Shared.Damage.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Damage.Components;

[Access(typeof(SharedDamageOtherOnHitSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DamageOtherOnHitComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool IgnoreResistances = false;

    [DataField(required: true), AutoNetworkedField]
    public DamageSpecifier Damage = default!;

}
