namespace Content.Server.CartridgeLoader.Cartridges;

[RegisterComponent]
public sealed partial class NewsReaderCartridgeComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public int ArticleNumber;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public bool NotificationOn = true;
}
