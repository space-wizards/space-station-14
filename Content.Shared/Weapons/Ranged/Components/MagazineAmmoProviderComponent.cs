using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio;

namespace Content.Shared.Weapons.Ranged;

/// <summary>
/// Wrapper around a magazine (handled via ItemSlot). Passes all AmmoProvider logic onto it.
/// </summary>
[RegisterComponent, Virtual]
[Access(typeof(SharedGunSystem))]
public partial class MagazineAmmoProviderComponent : AmmoProviderComponent
{
    [ViewVariables(VVAccess.ReadWrite), DataField("soundAutoEject")]
    public SoundSpecifier? SoundAutoEject = new SoundCollectionSpecifier("MagazineAmmoAutoEjectSound");

    /// <summary>
    /// Should the magazine automatically eject when empty.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("autoEject")]
    public bool AutoEject = false;
}
