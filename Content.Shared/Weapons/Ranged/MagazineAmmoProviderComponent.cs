using Content.Shared.Sound;

namespace Content.Shared.Weapons.Ranged;

[RegisterComponent]
public sealed class MagazineAmmoProviderComponent : AmmoProviderComponent
{
    [ViewVariables(VVAccess.ReadWrite), DataField("soundAutoEject")]
    public SoundSpecifier? SoundAutoEject = new SoundPathSpecifier("/Audio/Weapons/Guns/EmptyAlarm/smg_empty_alarm.ogg");

    /// <summary>
    /// Should the magazine automatically eject when empty.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("autoEject")]
    public bool AutoEject = false;
}
