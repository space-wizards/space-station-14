using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
///     Simply provides a certain capacity of entities that cannot be reloaded through normal means and have
///     no special behavior like cycling, magazine
/// </summary>
[RegisterComponent]
public sealed class BasicEntityAmmoProviderComponent : AmmoProviderComponent
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("proto", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Proto = default!;

    /// <summary>
    ///     Max capacity.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("capacity")]
    public int? Capacity = null;

    /// <summary>
    ///     Actual ammo left. Initialized to capacity unless they are non-null and differ.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("count")]
    public int? Count = null;
}

[Serializable, NetSerializable]
public sealed class BasicEntityAmmoProviderComponentState : ComponentState
{
    public int? Capacity;
    public int? Count;

    public BasicEntityAmmoProviderComponentState(int? capacity, int? count)
    {
        Capacity = capacity;
        Count = count;
    }
}
