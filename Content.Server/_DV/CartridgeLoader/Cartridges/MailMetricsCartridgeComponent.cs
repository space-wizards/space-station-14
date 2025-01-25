namespace Content.Server._DV.CartridgeLoader.Cartridges;

[RegisterComponent, Access(typeof(MailMetricsCartridgeSystem))]
public sealed partial class MailMetricsCartridgeComponent : Component
{
    /// <summary>
    /// Station entity keeping track of logistics stats
    /// </summary>
    [DataField]
    public EntityUid? Station;
}
