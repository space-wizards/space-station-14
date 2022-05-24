using Content.Shared.Sound;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;

namespace Content.Shared.Weapons.Ranged;

[RegisterComponent]
public sealed class MagazineAmmoProviderComponent : AmmoProviderComponent
{
    [ViewVariables(VVAccess.ReadWrite), DataField("soundMagInsert")]
    public SoundSpecifier? SoundMagInsert;

    [ViewVariables(VVAccess.ReadWrite), DataField("soundMagEject")]
    public SoundSpecifier? SoundMagEject;

    [ViewVariables(VVAccess.ReadWrite), DataField("soundAutoEject")]
    public SoundSpecifier? SoundAutoEject = new SoundPathSpecifier("/Audio/Weapons/Guns/EmptyAlarm/smg_empty_alarm.ogg");

    [ViewVariables, DataField("whitelist")]
    public EntityWhitelist? Whitelist;

    public ContainerSlot Magazine = default!;

    /// <summary>
    /// Should the magazine automatically eject when empty.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("autoEject")]
    public bool AutoEject = false;
}
