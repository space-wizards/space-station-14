using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.Fax;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FaxecuteComponent : Component
{

    /// <summary>
    /// Type of damage dealt when faxecuted.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier Damage = new();
}

