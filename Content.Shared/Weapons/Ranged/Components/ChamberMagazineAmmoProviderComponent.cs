using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// Chamber + mags in one package. If you need just magazine then use <see cref="MagazineAmmoProviderComponent"/>
/// </summary>
[RegisterComponent, AutoGenerateComponentState]
[Access(typeof(SharedGunSystem))]
public sealed partial class ChamberMagazineAmmoProviderComponent : MagazineAmmoProviderComponent
{
    /// <summary>
    /// If the gun has a bolt and whether that bolt is closed. Firing is impossible
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("boltClosed"), AutoNetworkedField]
    public bool? BoltClosed = false;

    /// <summary>
    /// Does the gun automatically open and close the bolt upon shooting.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("autoCycle"), AutoNetworkedField]
    public bool AutoCycle = true;

    /// <summary>
    /// Can the gun be racked, which opens and then instantly closes the bolt to cycle a round.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("canRack"), AutoNetworkedField]
    public bool CanRack = true;

    [ViewVariables(VVAccess.ReadWrite), DataField("soundBoltClosed"), AutoNetworkedField]
    public SoundSpecifier? BoltClosedSound = new SoundCollectionSpecifier("ChamberMagazineClosedSound");

    [ViewVariables(VVAccess.ReadWrite), DataField("soundBoltOpened"), AutoNetworkedField]
    public SoundSpecifier? BoltOpenedSound = new SoundCollectionSpecifier("ChamberMagazineOpenedSound");

    [ViewVariables(VVAccess.ReadWrite), DataField("soundRack"), AutoNetworkedField]
    public SoundSpecifier? RackSound = new SoundCollectionSpecifier("ChamberMagazineReloadSound");
}
