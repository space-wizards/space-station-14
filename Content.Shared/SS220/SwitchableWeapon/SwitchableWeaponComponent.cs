// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Robust.Shared.Audio;

namespace Content.Shared.SS220.SwitchableWeapon;

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

    [ViewVariables(VVAccess.ReadWrite)][DataField("isOpen")]
    public bool IsOpen = false;

    [ViewVariables(VVAccess.ReadWrite)][DataField("openSound")]
    public SoundSpecifier? OpenSound;

    [ViewVariables(VVAccess.ReadWrite)][DataField("closeSound")]
    public SoundSpecifier? CloseSound;

    [ViewVariables(VVAccess.ReadWrite)][DataField("sizeOpened")]
    public int SizeOpened = 15;

    [ViewVariables(VVAccess.ReadWrite)][DataField("sizeClosed")]
    public int SizeClosed = 5;
}
