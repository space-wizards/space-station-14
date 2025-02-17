
using Content.Shared.Damage;
using Content.Shared.Item;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.SwitchableWeapon;

[RegisterComponent]
public sealed partial class SwitchableWeaponComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)][DataField("damageFolded")]
    public DamageSpecifier DamageFolded = new(){
        DamageDict = new()
        {
            { "Blunt", 0.0f },
        }
    };

    [ViewVariables(VVAccess.ReadWrite)][DataField("damageOpen")]
    public DamageSpecifier DamageOpen = new(){
        DamageDict = new()
        {
            { "Blunt", 4.0f },
        }
    };

    [ViewVariables(VVAccess.ReadWrite)][DataField("staminaDamageFolded")]
    public float StaminaDamageFolded = 0;

    [ViewVariables(VVAccess.ReadWrite)][DataField("staminaDamageOpen")]
    public float StaminaDamageOpen = 28;

    [ViewVariables(VVAccess.ReadWrite)][DataField("isOpen")]
    public bool IsOpen = false;

    [ViewVariables(VVAccess.ReadWrite)][DataField("openSound")]
    public SoundSpecifier? OpenSound;

    [ViewVariables(VVAccess.ReadWrite)][DataField("closeSound")]
    public SoundSpecifier? CloseSound;

    [ViewVariables(VVAccess.ReadWrite)][DataField("bonkSound")]
    public SoundSpecifier? BonkSound;

    [ViewVariables(VVAccess.ReadWrite)][DataField("sizeOpened")]
    public ProtoId<ItemSizePrototype> SizeOpened = "Normal";

    [ViewVariables(VVAccess.ReadWrite)][DataField("sizeClosed")]
    public ProtoId<ItemSizePrototype> SizeClosed = "Small";
}
