namespace Content.Server.CartridgeLoader.Cartridges;

[RegisterComponent]
public sealed partial class NewsReaderCartridgeComponent : Component
{
    /// <summary>
    /// The cartridge loader the news reader cartridge is contained/installed in
    /// </summary>
    [DataField("cartridgeLoader")]
    public EntityUid? CartridgeLoader;

    [ViewVariables(VVAccess.ReadWrite)]
    public int ArticleNumber;

    [ViewVariables(VVAccess.ReadWrite), DataField("notificationOn")]
    public bool NotificationOn = true;
}
