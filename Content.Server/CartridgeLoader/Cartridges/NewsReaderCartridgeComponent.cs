namespace Content.Server.CartridgeLoader.Cartridges;

[RegisterComponent]
public sealed class NewsReaderCartridgeComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public int ArticleNumber;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool NotificationOn = true;
}
