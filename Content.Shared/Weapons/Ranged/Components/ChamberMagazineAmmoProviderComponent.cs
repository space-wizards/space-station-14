namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// Chamber + mags in one package. If you need just magazine then use <see cref="MagazineAmmoProviderComponent"/>
/// </summary>
[RegisterComponent, AutoGenerateComponentState]
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
    public bool AutoCycle;
}
