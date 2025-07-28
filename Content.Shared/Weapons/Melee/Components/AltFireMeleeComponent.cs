using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Melee.Components;

/// <summary>
/// This is used to allow ranged weapons to make melee attacks by right-clicking.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedMeleeWeaponSystem))]
public sealed partial class AltFireMeleeComponent : Component
{
    [DataField, AutoNetworkedField]
    public AltFireAttackType AttackType = AltFireAttackType.Light;
}


[Flags]
public enum AltFireAttackType : byte
{
    Light = 0, // Standard single-target attack.
    Heavy = 1 << 0, // Wide swing.
    Disarm = 1 << 1
}
