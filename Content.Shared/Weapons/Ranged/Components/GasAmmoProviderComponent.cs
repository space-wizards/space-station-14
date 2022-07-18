using Content.Shared.Sound;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Weapons.Ranged;

/// <summary>
/// Wrapper around a magazine (handled via ItemSlot). Passes all AmmoProvider logic onto it.
/// </summary>
[RegisterComponent, Virtual]
public class GasAmmoProviderComponent : AmmoProviderComponent
{
    [ViewVariables]
    public EntityUid? TankEntity;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("proto", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Proto = default!;

    [ViewVariables(VVAccess.ReadWrite), DataField("gasId")]
    public int GasId = 0;

    [ViewVariables(VVAccess.ReadOnly)]
    public float Moles = 0;

    [ViewVariables(VVAccess.ReadWrite), DataField("molesPerShot")]
    public float MolesPerShot = 0.2f;
}

[Serializable, NetSerializable]
public sealed class GasAmmoProviderComponentState : ComponentState
{
    public float Moles;

    public float MolesPerShot;

    public GasAmmoProviderComponentState(float moles, float molesPerShot)
    {
        Moles = moles;
        MolesPerShot = molesPerShot;
    }
}
