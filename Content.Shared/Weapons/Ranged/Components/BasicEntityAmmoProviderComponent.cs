using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
///     Simply provides a certain capacity of entities that cannot be reloaded through normal means and have
///     no special behavior like cycling, magazine
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BasicEntityAmmoProviderComponent : AmmoProviderComponent
{
    [DataField]
    public EntProtoId Proto = default!;

    /// <summary>
    ///     Max capacity.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("capacity")]
    [AutoNetworkedField]
    public int? Capacity = null;

    /// <summary>
    ///     Actual ammo left. Initialized to capacity unless they are non-null and differ.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("count")]
    [AutoNetworkedField]
    public int? Count = null;
}
